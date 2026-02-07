using System.Text.Json.Serialization;

namespace Profily.Core.Models.TechStack;

/// <summary>
/// Technologies grouped by category — the structured shape consumed by frontends/APIs.
/// Built from a flat list of DetectedTechnology after aggregation.
/// </summary>
public sealed class CategorizedTechStack
{
    [JsonPropertyName("languages")]
    public List<DetectedTechnology> Languages { get; set; } = [];

    [JsonPropertyName("frameworks")]
    public List<DetectedTechnology> Frameworks { get; set; } = [];

    [JsonPropertyName("libraries")]
    public List<DetectedTechnology> Libraries { get; set; } = [];

    [JsonPropertyName("tools")]
    public List<DetectedTechnology> Tools { get; set; } = [];

    [JsonPropertyName("databases")]
    public List<DetectedTechnology> Databases { get; set; } = [];

    [JsonPropertyName("others")]
    public List<DetectedTechnology> Others { get; set; } = [];

    /// <summary>
    /// Build from a flat list of technologies, grouping by category.
    /// </summary>
    public static CategorizedTechStack FromFlat(List<DetectedTechnology> technologies)
    {
        var grouped = technologies.ToLookup(t => t.Category);

        return new CategorizedTechStack
        {
            Languages = [.. grouped[TechCategory.Language]],
            Frameworks = [.. grouped[TechCategory.Framework]],
            Libraries = [.. grouped[TechCategory.Library]],
            Tools = [.. grouped[TechCategory.Tool]],
            Databases = [.. grouped[TechCategory.Database]],
            Others = [.. grouped[TechCategory.Other]]
        };
    }
}
