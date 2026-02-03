using System.Text.Json.Serialization;

namespace Profily.Core.Models;

public sealed class User : CosmosDocument
{
    public const string DocumentType = "user";

    [JsonPropertyName("type")]
    public override string Type => DocumentType;

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
}