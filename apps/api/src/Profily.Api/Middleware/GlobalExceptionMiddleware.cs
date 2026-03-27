using Profily.Core.Exceptions;

namespace Profily.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            Core.Exceptions.ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ProPlanRequiredException => (StatusCodes.Status403Forbidden, "Pro Plan Required"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        // 5xx = Error with full stack trace, 4xx = Warning with message only
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Client error {StatusCode} on {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        // Don't overwrite if response already started (e.g. SSE streaming)
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new Dictionary<string, object?>
        {
            ["type"] = "https://tools.ietf.org/html/rfc7807",
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = statusCode < 500 ? exception.Message : "An unexpected error occurred"
        };

        // Add validation errors if applicable
        if (exception is Core.Exceptions.ValidationException validationEx)
        {
            problem["errors"] = validationEx.Errors;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
