using Profily.Core.Models.Logging;

namespace Profily.Api.Extensions;

public static class HttpContextExtensions
{
    private const string WideEventKey = "WideEvent";

    /// <summary>
    /// Gets the wide event for the current request.
    /// Return null if middleware hasn't initialized it yet.
    /// </summary>
    public static WideEvent? GetWideEvent(this HttpContext context)
    {
        return context.Items.TryGetValue(WideEventKey, out var value) 
            ? value as WideEvent 
            : null; 
    }

    /// <summary>
    /// Sets the wide event for the current request (called by middleware).
    /// </summary>
    public static void SetWideEvent(this HttpContext context, WideEvent wideEvent)
    {
        context.Items[WideEventKey] = wideEvent;
    }
}