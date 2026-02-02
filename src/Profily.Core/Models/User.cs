using System.Text.Json.Serialization;

namespace Profily.Core.Models;

public class User
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Partition key - mirrors Id for Cosmos DB partitioning.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId => Id;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "user";

    [JsonPropertyName("githubId")]
    public long GitHubId { get; set; }

    [JsonPropertyName("githubUsername")]
    public required string GitHubUsername { get; set; }

    /// <summary>
    /// GitHub display name (may differ from username).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}