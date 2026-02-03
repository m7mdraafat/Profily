// Profily.Core/Models/Profile/ProfileConfig.cs

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Profily.Core.Models.Profile;

/// <summary>
/// User's personalized profile configuration.
/// One per user - created when user starts building, updated on each save.
/// 
/// Design notes:
/// - Stores user's content (about me, skills, etc.)
/// - Tracks template used (for template popularity)
/// - Maintains deploy state and history snapshot
/// </summary>
public sealed class ProfileConfig : CosmosDocument
{
    [JsonIgnore]
    public const string DocumentType = "profileConfig";
    
    [JsonPropertyName("type")]
    public override string Type => DocumentType;

    /// <summary>
    /// Reference to base template. Null if started from scratch.
    /// Used for template usage analytics.
    /// </summary>
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// User's selected theme. Copied from template, can be changed independently.
    /// </summary>
    [JsonPropertyName("theme")]
    [Required]
    public required ThemeConfig Theme { get; set; }

    /// <summary>
    /// User's customized section configuration.
    /// Copied from template, user can add/remove/reorder.
    /// </summary>
    [JsonPropertyName("sections")]
    [Required]
    public required List<TemplateSectionConfig> Sections { get; set; }

    /// <summary>
    /// User-provided content for profile sections.
    /// </summary>
    [JsonPropertyName("content")]
    [Required]
    public required ProfileContent Content { get; set; }

    /// <summary>
    /// Social media and contact links.
    /// </summary>
    [JsonPropertyName("socialLinks")]
    public SocialLinks? SocialLinks { get; set; }

    /// <summary>
    /// Feature toggles and display preferences.
    /// </summary>
    [JsonPropertyName("preferences")]
    public ProfilePreferences Preferences { get; set; } = new();

    /// <summary>
    /// True until first successful deploy.
    /// Used to show "unsaved changes" indicator in UI.
    /// </summary>
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; } = true;

    /// <summary>
    /// Timestamp of last successful deploy.
    /// Null if never deployed.
    /// </summary>
    [JsonPropertyName("lastDeployedAt")]
    public DateTime? LastDeployedAt { get; set; }

    /// <summary>
    /// Snapshot of config at last deploy for diff comparison.
    /// Enables "changes since deploy" feature.
    /// </summary>
    [JsonPropertyName("lastDeployedConfigSnapshot")]
    public JsonDocument? LastDeployedConfigSnapshot { get; set; }

    public static ProfileConfig CreateForUser(string userId, ProfileTemplate template) => new()
    {
        Id = $"{DocumentType}-{userId}",
        UserId = userId,
        TemplateId = template.Id,
        Theme = template.Theme,
        Sections = template.Sections.ToList(), // Deep copy via record semantics
        Content = new ProfileContent()
    };
    
    public static ProfileConfig CreateEmpty(string userId, ThemeConfig defaultTheme) => new()
    {
        Id = $"{DocumentType}-{userId}",
        UserId = userId,
        Theme = defaultTheme,
        Sections = [],
        Content = new ProfileContent()
    };
}

/// <summary>
/// User-provided content for profile sections.
/// All fields optional - sections render with placeholders if missing.
/// </summary>
public sealed class ProfileContent
{
    [JsonPropertyName("displayName")]
    [StringLength(100)]
    public string? DisplayName { get; set; }

    [JsonPropertyName("tagline")]
    [StringLength(200)]
    public string? Tagline { get; set; }

    [JsonPropertyName("aboutMe")]
    [StringLength(2000)]
    public string? AboutMe { get; set; }

    /// <summary>
    /// Skills/technologies not auto-detected from repos.
    /// </summary>
    [JsonPropertyName("customSkills")]
    public List<string>? CustomSkills { get; set; }

    /// <summary>
    /// Repository names to feature. Matched against user's repos.
    /// </summary>
    [JsonPropertyName("pinnedRepoNames")]
    public List<string>? PinnedRepoNames { get; set; }
}

/// <summary>
/// Social media and contact links.
/// URL validation ensures proper formatting.
/// </summary>
public sealed class SocialLinks
{
    [JsonPropertyName("linkedin")]
    [Url]
    public string? Linkedin { get; set; }

    [JsonPropertyName("twitter")]
    [Url]
    public string? Twitter { get; set; }

    [JsonPropertyName("youtube")]
    [Url]
    public string? Youtube { get; set; }

    [JsonPropertyName("discord")]
    public string? Discord { get; set; } // Discord is username, not URL

    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }

    [JsonPropertyName("website")]
    [Url]
    public string? Website { get; set; }

    [JsonPropertyName("leetcode")]
    [Url]
    public string? Leetcode { get; set; }

    [JsonPropertyName("resume")]
    [Url]
    public string? Resume { get; set; }
}

/// <summary>
/// Feature toggles for optional profile elements.
/// </summary>
public sealed class ProfilePreferences
{
    /// <summary>
    /// Show "Open to Work" badge. User controls visibility.
    /// </summary>
    [JsonPropertyName("showOpenToWork")]
    public bool ShowOpenToWork { get; set; }

    /// <summary>
    /// Show profile view counter. Default true for engagement.
    /// </summary>
    [JsonPropertyName("showProfileViews")]
    public bool ShowProfileViews { get; set; } = true;
}