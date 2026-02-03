using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Profily.Core.Models.Profile;

/// <summary>
/// Represents a modular section that can be included in a user's profile.
/// Examples: Header, Stats, Tech Stack, Contact Links.
/// </summary>
public sealed class Section : CosmosDocument
{
    [JsonIgnore]
    public const string DocumentType = "section";

    [JsonPropertyName("type")]
    public override string Type => DocumentType;

    /// <summary>
    /// URL-safe identifier. Used in APIs and template references.
    /// Example: "header", "github-stats", "tech-stack"
    /// </summary>
    [JsonPropertyName("slug")]
    [Required]
    [StringLength(50, MinimumLength = 3, 
        ErrorMessage = "Slug must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", 
        ErrorMessage = "Slug must be lowercase alphanumeric with hyphens")]
    public required string Slug { get; set; }

    /// <summary>
    /// Human-readable name shown in UI.
    /// Example: "GitHub Stats", "Tech Stack"
    /// </summary>
    [JsonPropertyName("displayName")]
    [Required]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "Display name must be between 3 and 100 characters")]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Explains what this section does. Shown in section picker UIs.
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(500, 
        ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Emoji or icon identifier for visual representation.
    /// </summary>
    [JsonPropertyName("icon")]
    [StringLength(50, 
        ErrorMessage = "Icon identifier cannot exceed 50 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Default ordering in template. Lower = appears earlier.
    /// </summary>
    [JsonPropertyName("sortOrder")]
    [Range(0, 100, 
        ErrorMessage = "Sort order must be between 0 and 100")]
    public int SortOrder { get; set; }

    /// <summary>
    /// Data fields needed to render this section.
    /// Used to validate user has required data before enable section.
    /// Example: ["username", "displayName"], ["repositories"]
    /// </summary>
    [JsonPropertyName("requiredDataFields")]
    public List<string> RequiredDataFields { get; set; } = [];

    /// <summary>
    /// Soft delete flag. Inactive sections hidden from UI but preserved in DB.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Factory method for creating system sections with consistent ID format.
    /// </summary>
    public static Section CreateSystemSection(string slug, string displayName) => new()
    {
        Id = $"{DocumentType}-{slug}",
        UserId = "system",
        Slug = slug,
        DisplayName = displayName
    };
}