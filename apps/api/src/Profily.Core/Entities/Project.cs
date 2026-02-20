namespace Profily.Core.Entities;

/// <summary>
/// A GitHub repository synced from the user's account.
/// Stored in its own table (unlike skills) because:
/// - There can be 100+ repos per user — too large for jsonb
/// - We need to query across users: "all projects using C#"
/// - Individual repo updates (custom descriptions) are common
/// - isSelected + displayOrder are portfolio-specific state
/// </summary>
public sealed class Project
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // GitHub data (synced)
    public long GitHubRepoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDescriptionEdited { get; set; } // if true, sync won't overwrite
    public string? Language { get; set; }
    public string[] Topics { get; set; } = [];
    public int Stars { get; set; }
    public int Forks { get; set; }
    public bool IsFork { get; set; }
    public bool IsArchived { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? HomepageUrl { get; set; }
    public DateTimeOffset? LastPushedAt { get; set; }
    public DateTimeOffset SyncedAt { get; set; }

    // Portfolio display
    public bool IsSelected { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
