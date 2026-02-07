using Microsoft.AspNetCore.Http;
using Profily.Core.Interfaces;
using Profily.Core.Models.Logging;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Bridges HttpContext to the WideEvent for infrastructure services.
/// Registered as scoped so each request gets its own instance.
/// Reads directly from HttpContext.Items using the same key the middleware uses.
/// </summary>
public sealed class HttpWideEventAccessor : IWideEventAccessor
{
    private const string WideEventKey = "WideEvent";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpWideEventAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public WideEvent? WideEvent
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null) return null;

            return context.Items.TryGetValue(WideEventKey, out var value)
                ? value as WideEvent
                : null;
        }
    }
}
