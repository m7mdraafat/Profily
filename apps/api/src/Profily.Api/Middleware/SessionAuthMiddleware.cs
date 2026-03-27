using Microsoft.EntityFrameworkCore;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;

namespace Profily.Api.Middleware;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/github",
        "/api/auth/callback",
        "/api/payments/webhook",
        "/health"
    };

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService, ProfilyDbContext dbContext, ILogger<SessionAuthMiddleware> logger)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for public endpoint
        if (_skipPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // Skip non-API paths
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Read session cookie
        if (!context.Request.Cookies.TryGetValue("session", out var sessionIdStr) || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            logger.LogWarning("Missing or invalid session cookie on {Method} {Path}", context.Request.Method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new { type = "https://tools.ietf.org/html/rfc7807", title = "Unauthorized", status = 401, detail = "Authentication required." });
            return;
        }

        // Validate session
        var session = await sessionService.GetValidAsync(sessionId, context.RequestAborted);
        if (session is null)
        {
            logger.LogWarning("Expired or invalid session on {Method} {Path}", context.Request.Method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Load user
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == session.UserId, context.RequestAborted);
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Attach user to context for endpoints to use
        context.Items["UserId"] = user.Id;
        context.Items["User"] = user;

        // Extend session (sliding expiration) - fire and forget to not block the response
        await sessionService.ExtendAsync(session.Id, context.RequestAborted);

        // Add UserId to all downstream log messages
        using (logger.BeginScope(new Dictionary<string, object> { ["UserId"] = user.Id, ["Username"] = user.Username }))
        {
            await _next(context);
        }
    }
}