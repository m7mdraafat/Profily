namespace Profily.Core.Exceptions;

/// <summary>
/// Thrown when a GitHub API call fails.
/// Middleware maps this to HTTP 429 (rate limited) or 502 (other errors).
/// </summary>
public sealed class GitHubApiException : ProfilyException
{
    public int GitHubStatusCode { get; }
    public int? RateLimitRemaining { get; }
    public DateTimeOffset? RateLimitReset { get; }

    public GitHubApiException(string message, int githubStatusCode)
        : base("GITHUB_API_ERROR", message)
    {
        GitHubStatusCode = githubStatusCode;
    }

    public GitHubApiException(
        string message,
        int githubStatusCode,
        int rateLimitRemaining,
        DateTimeOffset rateLimitReset)
        : base("GITHUB_API_ERROR", message)
    {
        GitHubStatusCode = githubStatusCode;
        RateLimitRemaining = rateLimitRemaining;
        RateLimitReset = rateLimitReset;
    }

    public GitHubApiException(string message, int githubStatusCode, Exception innerException)
        : base("GITHUB_API_ERROR", message, innerException)
    {
        GitHubStatusCode = githubStatusCode;
    }
}
