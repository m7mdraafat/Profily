using System.Text.Json.Serialization;

namespace Profily.Core.Models;

/// <summary>
/// Base class for all Cosmos DB documents. Enforces:
/// - Consistent ID and partition key handling.
/// - Type discriminator for single-container design.
/// - Audit fields for created/updated timestamps.
/// </summary>
public abstract class CosmosDocument
{
    /// <summary>
    /// Unique document identifier. Format varies by type:
    /// - System data: "{type}-{slug}" (e.g. "section-header")
    /// - User data: "{type}-{userId}" (e.g. "user-12345")
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id {get; set;}

    /// <summary>
    /// Partition key - determines data distribution.
    /// - System data: "system" (co-locates all templates/section for efficient queries)
    /// - User data: actual userId (isolates user data, enable efficient user-specific queries)
    /// </summary>
    [JsonPropertyName("userId")]
    public required string UserId {get; set;}

    /// <summary>
    /// Document type discriminator for single-container design.
    /// Used in queries to filter by document type.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type {get;}

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}