using System.Text.Json.Serialization;

namespace Profily.Core.Models.TechStack;

/// <summary>
/// A single technology detected from analyzing a user's GitHub repositories.
/// </summary>
public sealed class DetectedTechnology
{
    [JsonPropertyName("name")]
    public required string Name { get; set;}
    
    [JsonPropertyName("category")]
    public required TechCategory Category { get; set; }

    /// <summary>
    /// Optional icon slug for frontend/render display (e.g., "dotnet", "react").
    /// Resolved from framework-mappings.json or similar mapping logic.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TechCategory
{
    Language,
    Framework,
    Library,
    Tool,
    Database,
    Other
}