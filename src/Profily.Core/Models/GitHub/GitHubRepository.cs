using System.Text.Json.Serialization;

namespace Profily.Core.Models.GitHub;

/// <summary>
/// Represent a single GitHub repository.
/// </summary>
public class GitHubRepository
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; set; } // e.g. owner/repo

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("htmlUrl")]
    public required string HtmlUrl { get; set; }

    [JsonPropertyName("homePage")]
    public string? HomePage { get; set; } // custom page URL

    [JsonPropertyName("language")]
    public string? Language { get; set; } // primary language

    [JsonPropertyName("starsCount")]
    public int StarsCount { get; set; }

    [JsonPropertyName("forksCount")]
    public int ForksCount { get; set; }

    [JsonPropertyName("isFork")]
    public bool IsFork { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; } // size in KB

    [JsonPropertyName("topics")]
    public List<string>? Topics { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("pushedAt")]
    public DateTime? PushedAt { get; set; }
}
