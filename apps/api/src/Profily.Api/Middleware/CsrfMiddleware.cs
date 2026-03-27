namespace Profily.Api.Middleware;

public sealed class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _allowedOrigin;

    private readonly ILogger<CsrfMiddleware> _logger;

    public CsrfMiddleware(RequestDelegate next, string allowedOrigin, ILogger<CsrfMiddleware> logger)
    {
        _next = next;
        _allowedOrigin = allowedOrigin;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check mutating methods
        var method = context.Request.Method;
        if (HttpMethods.IsPost(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPut(method))
        {
            // Skip for webhook (no cookie, uses HMAC)
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/api/payments/webhook", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var origin = context.Request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origin) && origin != _allowedOrigin)
            {
                _logger.LogWarning("CSRF rejected: origin {Origin} on {Method} {Path}", origin, method, path);
                context.Response.StatusCode = 403;
                return;
            }
        }

        await _next(context);
    }
}