using Profily.Core.Models.Logging;

namespace Profily.Core.Interfaces;

/// <summary>
/// Provides access to the current request's wide event for enrichment.
/// Infrastructure services use this to add business context without
/// depending on HttpContext directly.
/// </summary>
public interface IWideEventAccessor
{
    /// <summary>
    /// Gets the wide event for the current request, or null if not in an HTTP context.
    /// </summary>
    WideEvent? WideEvent { get; }
}
