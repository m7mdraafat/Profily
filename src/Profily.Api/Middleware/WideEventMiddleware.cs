using System.Reflection;
using System.Security.Claims;
using Profily.Api.Extensions;
using Profily.Core.Models.Logging;

namespace Profily.Api.Middleware;

/// <summary>
/// Middleware to add a custom header to all responses for wide event tracking.
/// </summary>
public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WideEventMiddleware> _logger;
    private readonly string _serviceName;
    private readonly string _serviceVersion;

    public WideEventMiddleware(
        RequestDelegate next,
        ILogger<WideEventMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _serviceName = "profily-api";
        _serviceVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "unknown";
    }

    public async Task InvokeAsync(HttpContext context, IHostEnvironment environment)
    {
        var wideEvent = new WideEvent();

        // 1. Request context (known immediately)
        wideEvent.SetRequestContext(
            method: context.Request.Method,
            path: context.Request.Path.Value ?? "/",
            requestId: context.TraceIdentifier,
            userAgent: context.Request.Headers.UserAgent.ToString(),
            clientIp: context.Connection.RemoteIpAddress?.ToString()
        );

        // 2. Environment context
        wideEvent.SetEnvironmentContext(
            serviceName: _serviceName,
            version: _serviceVersion,
            environment: environment.EnvironmentName
        );

        // 3. Make it available for endpoint enrichment
        context.SetWideEvent(wideEvent);

        try
        {
            await _next(context);

            // 4. User context (available after auth middleware)
            EnrichWithUserContext(context, wideEvent);

            // 5. Complete with status code.
            wideEvent.Complete(context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            EnrichWithUserContext(context, wideEvent);
            wideEvent.SetErrorContext(ex);
            wideEvent.Complete(500);

            // Set TraceId on error responses so clients can reference/correlate it
            context.Items["TraceId"] = context.TraceIdentifier;

            throw; // Let the framework handle the response.
        }
        finally
        {
            // 6. Emit ONE canonical log entry for the entire request lifecycle.
            EmitWideEvent(wideEvent);
        }
    }

    private static void EnrichWithUserContext(HttpContext context, WideEvent wideEvent)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = context.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;

        wideEvent.SetUserContext(userId, username, email);
    }

    private void EmitWideEvent(WideEvent wideEvent)
    {
        // Emit as a single structured log entry with all fields.
        // Serilog destructures the dictionary into top-level properties.
        _logger.LogInformation("{@WideEvent}", wideEvent.Data);
    }
}