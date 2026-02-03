using Profily.Core.Models.GitHub;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for fetching GitHub data via GitHub API
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Gets all public repositories for the authenticated user.
    /// </summary>
    Task<List<GitHubRepository>> GetUserRepositoriesAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for the authenticated user.
    /// </summary>
    Task<GitHubStats> GetUserStatsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets language breakdown for a specific repository.
    /// </summary>
    Task<List<LanguageStat>> GetRepositoryLanguagesAsync(
        string accessToken,
        string owner,
        string repoName,
        CancellationToken cancellationToken = default);
}