namespace Profily.Core.ValueObjects;

/// <summary>
/// A value object representing a detected or user-edited skill.
/// Stored as jsonb array inside the User entity — not a separate table.
/// 
/// Why jsonb and not a Skills table?
/// - Skills are always read/written with the user (no independent access pattern)
/// - The full list is replaced atomically on edit (no partial updates)
/// - PostgreSQL jsonb supports indexing and querying (WHERE skills @> '[{"name":"C#"}]')
/// - Avoids N+1 queries and JOINs for a simple list
/// </summary>
public sealed class InferredSkill
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // frontend, backend, devops, database, ai, tools
    public int Confidence { get; set; } // 1-99
    public int RepoCount { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsUserEdited { get; set; }
    public DateTimeOffset? LastUsed { get; set; }
}
