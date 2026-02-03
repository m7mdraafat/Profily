using System.Text.Json.Serialization;

namespace Profily.Core.Models.GitHub;

/// <summary>
/// Represents statistics for a programming language used in repositories.
/// </summary>
public class LanguageStat
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("bytes")]
    public required long Bytes { get; set; }
    [JsonPropertyName("percentage")]
    public required double Percentage { get; set; }
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}