using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Profily.Core.Models.Profile;

public sealed class SectionStyle : CosmosDocument
{
    [JsonIgnore]
    public const string DocumentType = "sectionStyle";
    public override string Type => DocumentType;
    
    /// <summary>
    /// Reference to parent section. Enables querying styles by section.
    /// </summary>
    [JsonPropertyName("sectionId")]
    [Required]
    public required string SectionId { get; set; }

    /// <summary>
    /// URL-safe identifier within the section.
    /// Combined with section forms unique style reference: "stats/cards"
    /// </summary>
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

    /// <summary>
    /// URL to preview image showing this style's appearance.
    /// Enables visual selection in UIs.
    /// </summary>
    [JsonPropertyName("previewImageUrl")]
    [Url]
    public string? PreviewImageUrl { get; set; }

    /// <summary>
    /// Markdown template with Hundlebars-style placeholders.
    /// Placeholders: {{username}}, {{repoCount}}, etc.
    /// Defines how the section is rendered in the profile.
    /// </summary>
    [JsonPropertyName("markdownTemplate")]
    [Required]
    public required string MarkdownTemplate { get; set; }

    /// <summary>
    /// GitHub workflow files required by this style.
    /// Example: Snake animation needs scheduled workflow to regenerate image.
    /// Key: filename (e.g. ".github/workflows/snake.yml")
    ///</summary>
    [JsonPropertyName("workflows")]
    public List<WorkflowDefinition> Workflows { get; set; } = [];

    /// <summary>
    /// Static assets (SVGs, images) needed by this style.
    /// Deployed to user's repo alongside profile README.
    /// Key: filename (e.g. "assets/snake.svg")
    /// </summary>
    [JsonPropertyName("assets")]
    public List<AssetDefinition> Assets { get; set; } = [];

    /// <summary>
    /// JSON schema defining configuration options for this style.
    /// Enables dynamic form generation in UI.
    /// </summary>
    [JsonPropertyName("configSchema")]
    public JsonDocument? ConfigSchema { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    public static SectionStyle CreateSystem(string sectionSlug, string styleSlug, string displayName, string markdownTemplate) => new()
    {
        Id = $"{DocumentType}-{sectionSlug}-{styleSlug}",
        UserId = "system",
        SectionId = $"section-{sectionSlug}",
        Slug = styleSlug,
        DisplayName = displayName,
        MarkdownTemplate = markdownTemplate
    };
}

/// <summary>
/// Defines a GitHub Actions workflow file.
/// Immutable record - workflows are versioned with the style.
/// </summary>
public sealed record WorkflowDefinition
{
    /// <summary>
    /// Relative path within repository (e.g. ".github/workflows/snake.yml")
    /// </summary>
    [JsonPropertyName("path")]
    [Required]
    public required string Path { get; init; }

    /// <summary>
    /// Workflow YAML content with variable placeholders.
    /// </summary>
    [JsonPropertyName("content")]
    [Required]
    public required string Content { get; init; }
}

/// <summary>
/// Defines a static asset file to deploy
/// </summary>
public sealed record AssetDefinition
{

    [JsonPropertyName("path")]
    [Required]
    public required string Path { get; init; }

    /// <summary>
    /// Base64-encoded content for binary, raw string for text.
    /// </summary>
    [JsonPropertyName("content")]
    [Required]
    public required string Content { get; init; }
    /// <summary>
    /// MIME type hint: "svg", "png", "gif", "json", etc.
    /// </summary>
    [JsonPropertyName("contentType")]
    [Required]
    public required string ContentType { get; init; }
}