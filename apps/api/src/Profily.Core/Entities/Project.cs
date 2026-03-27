namespace Profily.Core.Entities;

public sealed class Project
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long GitHubRepoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CustomDescription { get; set; }
    public string? Language { get; set; }
    public List<string> Topics { get; set; } = [];
    public int Stars { get; set; }
    public int Forks { get; set; }
    public bool IsFork { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? HomepageUrl { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public string? SkillsHash { get; set; }
    public DateTime? SkillsInferredAt { get; set; }
    public DateTime? LastPushedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
