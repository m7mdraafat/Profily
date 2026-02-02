namespace Profily.Core.Options;

/// <summary>
/// Configuration options for CORS policies.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Allowed origins for CORS.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}