using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Profily.Core.Models.Profile;

public sealed class ProfileTemplate : CosmosDocument
{
    [JsonIgnore]
    public const string DocumentType = "template";

    [JsonPropertyName("type")]
    public override string Type => DocumentType;

    [JsonPropertyName("slug")]
    [Required]
    [StringLength(50, MinimumLength = 3, 
        ErrorMessage = "Slug must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", 
        ErrorMessage = "Slug must be lowercase alphanumeric with hyphens")]
    public required string Slug { get; set; }

    [JsonPropertyName("displayName")]
    [Required]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "Display name must be between 3 and 100 characters")]
    public required string DisplayName { get; set; }

    [JsonPropertyName("description")]
    [StringLength(500, 
        ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    [StringLength(10, 
        ErrorMessage = "Icon identifier cannot exceed 10 characters")]
    public string? Icon { get; set; }

    [JsonPropertyName("previewImageUrl")]
    [Url]
    public string? PreviewImageUrl { get; set; }

    /// <summary>
    /// Default theme applied when user selects this template.
    /// User can change theme independently after selection.
    /// </summary>
    [JsonPropertyName("theme")]
    public required ThemeConfig Theme { get; set; }

    /// <summary>
    /// Ordered list of sections with their default styles.
    /// Copied to user's config when template is selected.
    /// </summary>
    [JsonPropertyName("sections")]
    [Required]
    [MinLength(1, ErrorMessage = "At least one section is required")]
    public required List<TemplateSectionConfig> Sections { get; set; }

    /// <summary>
    /// True for admin-created templates, false for community (v2).
    /// Official templates shown first in gallery.
    /// </summary>
    [JsonPropertyName("isOfficial")]
    public bool IsOfficial { get; set; }

    [JsonPropertyName("createdBy")]
    public required string CreatedBy { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Tracks template popularity. Incremented on deploy, not selection.
    /// </summary>
    [JsonPropertyName("usageCount")]
    [Range(0, int.MaxValue, 
        ErrorMessage = "Usage count cannot be negative")]
    public int UsageCount { get; set; } = 0;

    public static ProfileTemplate CreateOfficial(string slug, string displayName, ThemeConfig theme, List<TemplateSectionConfig> sections) => new()
    {
        Id = $"{DocumentType}-{slug}",
        UserId = "system",
        Slug = slug,
        DisplayName = displayName,
        IsOfficial = true,
        Theme = theme,
        Sections = sections,
        CreatedBy = "admin",
        IsActive = true
    };
}


/// <summary>
/// Theme configuration for visual styling.
/// Separated from template to allow independent theme changes.
/// </summary>
public sealed record ThemeConfig
{
    [JsonPropertyName("id")]
    [Required]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    [Required]
    [StringLength(50)]
    public required string Name { get; init; }

    /// <summary>
    /// Primary accent color (hex). Used for links, highlights.
    /// </summary>
    [JsonPropertyName("primary")]
    [Required]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be valid hex color")]
    public required string Primary { get; init; }

    /// <summary>
    /// Secondary color for less prominent elements.
    /// </summary>
    [JsonPropertyName("secondary")]
    [Required]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public required string Secondary { get; init; }

    /// <summary>
    /// Background color for cards and containers.
    /// </summary>
    [JsonPropertyName("background")]
    [Required]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public required string Background { get; init; }

    /// <summary>
    /// Gradient string for capsule-render and similar services.
    /// Format: "0:COLOR1,100:COLOR2" or "auto"
    /// </summary>
    [JsonPropertyName("gradient")]
    public string? Gradient { get; init; }

    /// <summary>
    /// Text color override. Defaults based on background if null.
    /// </summary>
    [JsonPropertyName("textColor")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? TextColor { get; init; }
}

/// <summary>
/// Configuration for a section within a template.
/// Defines which style to use and section-specific settings.
/// </summary>
public sealed record TemplateSectionConfig
{
    [JsonPropertyName("sectionId")]
    [Required]
    public required string SectionId { get; init; }

    [JsonPropertyName("styleId")]
    [Required]
    public required string StyleId { get; init; }

    /// <summary>
    /// Display order. Lower = appears first in generated README.
    /// </summary>
    [JsonPropertyName("order")]
    [Range(0, 100)]
    public int Order { get; init; }

    /// <summary>
    /// Toggle to include/exclude from generated output.
    /// Allows users to disable sections without removing them.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Style-specific configuration. Schema defined by style's ConfigSchema.
    /// </summary>
    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; init; }
}
