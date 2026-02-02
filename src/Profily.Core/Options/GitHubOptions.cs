namespace Profily.Core.Options;

/// <summary>
/// Configuration options for GitHub integration.
/// </summary>
public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";
    
    /// <summary>
    /// The GitHub OAuth client ID.
    /// </summary>
    public required string ClientId { get; init; }
    
    /// <summary>
    /// The GitHub OAuth client secret.
    /// </summary>
    public required string ClientSecret { get; init; }
    
    /// <summary>
    /// OAuth scopes to request. Default: "read:user", "user:email"
    /// </summary>
    public string[] Scopes { get; init; } = ["read:user", "user:email"];
}