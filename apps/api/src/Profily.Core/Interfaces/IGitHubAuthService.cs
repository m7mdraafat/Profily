namespace Profily.Core.Interfaces;

public interface IGitHubAuthService
{
    string GetAuthorizationUrl(string state);
    Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default);
    Task<GitHubUserProfile> GetUserProfileAsync(string accessToken, CancellationToken ct = default);
}

public sealed class GitHubUserProfile
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
}