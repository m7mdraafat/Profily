using Profily.Core.Enums;

namespace Profily.Core.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public long GitHubId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string GitHubUrl { get; set; } = string.Empty;
    public byte[] GitHubTokenEncrypted { get; set; } = [];
    public int ReposCount { get; set; }
    public int FollowersCount { get; set; }
    public int ContributionsThisYear { get; set; }
    public PlanType Plan { get; set; } = PlanType.Free;
    public DateTime? PlanExpiresAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
