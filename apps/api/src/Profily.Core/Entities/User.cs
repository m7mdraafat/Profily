using Profily.Core.ValueObjects;
namespace Profily.Core.Entities;

/// <summary>
/// Registered user — maps to a GitHub account.
/// Skills and top languages are stored as jsonb/array directly on this entity
/// rather than in separate tables, because they are always accessed together
/// and editing them is a single atomic operation.
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }

    // GitHub identity
    public long GitHubId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string GitHubUrl { get; set; } = string.Empty;

    // Social links — URLs only, platform derived from domain at render time
    public string[] SocialLinks { get; set; } = [];

    // GitHub access token — AES-256 encrypted, never exposed in API responses
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    // Aggregated GitHub stats
    public int ReposCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int ContributionsThisYear { get; set; }
    public string[] TopLanguages { get; set; } = [];

    // Inferred skills — stored as jsonb in PostgreSQL
    public List<InferredSkill> Skills { get; set; } = [];

    // Timestamps
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public List<Project> Projects { get; set; } = [];
    public Portfolio? Portfolio { get; set; }
}
