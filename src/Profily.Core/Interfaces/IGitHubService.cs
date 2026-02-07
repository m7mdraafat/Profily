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

    /// <summary>
    /// Gets the full file tree of a repository in a single API call.
    /// Uses Git Trees API with recursive=1 to minimize API calls.
    /// </summary>
    Task<List<string>> GetRepoFileTreeAsync(
        string accessToken,
        string owner,
        string repoName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the raw content of a single file from a repository.
    /// Returns null if the file is not exists or is too large (>1MB).
    /// </summary>
    Task<string?> GetFileContentAsync(
        string accessToken,
        string owner,
        string repoName,
        string filePath,
        CancellationToken cancellationToken = default);
}