using System.Text.Json.Serialization;

namespace Profily.Core.Models.TechStack;

/// <summary>
/// Persisted result of analyzing a user's GitHub repos for technologies.
/// Stored in Cosmos DB with 6h memory cache as optimization layer.
/// </summary>
public sealed class TechStackProfile : CosmosDocument
{
    public const string DocumentType = "techStackProfile";

    [JsonPropertyName("type")]
    public override string Type => DocumentType;

    /// <summary>
    /// Technologies grouped by category for structured consumption.
    /// </summary>
    [JsonPropertyName("categorized")]
    public CategorizedTechStack Categorized { get; set; } = new();

    [JsonPropertyName("analyzedAt")]
    public DateTime AnalyzedAt { get; set; }

    [JsonPropertyName("analyzedRepoCount")]
    public int AnalyzedRepoCount { get; set; }

    /// <summary>
    /// Diagnostic: how many detections each signal produced.
    /// </summary>
    [JsonPropertyName("signalSummary")]
    public Dictionary<string, int> SignalSummary { get; set; } = new();

    public static TechStackProfile CreateForUser(string userId)
    {
        return new TechStackProfile
        {
            Id = $"{DocumentType}-{userId}",
            UserId = userId,
            AnalyzedAt = DateTime.UtcNow
        };
    }
}