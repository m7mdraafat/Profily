using System.Text.Json.Serialization;

namespace Profily.Core.Models.GitHub;

/// <summary>
/// Aggregated stats for a user's GitHub repositories.
/// </summary>
public class GitHubStats
{
    [JsonPropertyName("username")]
    public required string Username { get; set; }
    [JsonPropertyName("publicReposCount")]
    public int PublicReposCount { get; set; }
    [JsonPropertyName("totalStars")]
    public int TotalStars { get; set; }
    [JsonPropertyName("totalForks")]
    public int TotalForks { get; set; }
    [JsonPropertyName("followers")]
    public int Followers { get; set; }
    [JsonPropertyName("following")]
    public int Following { get; set; }

    [JsonPropertyName("totalCommits")]
    public int TotalCommits { get; set; } = 0;
    
    [JsonPropertyName("topLanguages")]
    public List<LanguageStat> TopLanguages { get; set; } = [];
    [JsonPropertyName("fetchedAt")]
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
