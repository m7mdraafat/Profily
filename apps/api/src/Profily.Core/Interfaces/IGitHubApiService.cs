using Profily.Core.Entities;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for interacting with the GitHub API to fetch user repositories and other related data.
/// </summary>
public interface IGitHubApiService
{
    Task<List<GitHubRepository>> GetRepositoriesAsync(string accessToken, CancellationToken ct = default);
}

public sealed class GitHubRepository
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public List<string> Topics { get; set; } = [];
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public bool Fork { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Homepage { get; set; }
    public DateTime? PushedAt { get; set; }
}