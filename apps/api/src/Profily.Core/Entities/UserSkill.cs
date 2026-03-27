using Profily.Core.Enums;

namespace Profily.Core.Entities;

public sealed class UserSkill
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }
    public decimal Confidence { get; set; }
    public string? IconFilename { get; set; }
    public SkillSource Source { get; set; } = SkillSource.Inferred;
    public Guid? SourceRepoId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
