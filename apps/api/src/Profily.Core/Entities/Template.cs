namespace Profily.Core.Entities;

/// <summary>
/// A portfolio template (3d-purple, minimal-clean, etc.).
/// Uses string ID (slug) instead of GUID because:
/// - Template IDs are human-readable and appear in URLs
/// - There are very few templates (3-10) — no performance concern
/// - Easier to reference in config files and seed data
/// </summary>
public sealed class Template
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? DemoHtml { get; set; }
    public string? LayoutUrl { get; set; }
    public string? CssUrl { get; set; }
    public string? JsUrl { get; set; }
    public string[] Features { get; set; } = [];
    public string[] AvailableSections { get; set; } = [];
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public List<Portfolio> Portfolios { get; set; } = [];
}
