using Profily.Core.Enums;

namespace Profily.Core.Entities;
/// A user's portfolio — the combination of template + customizations + selected projects.
/// One per user in MVP (enforced by unique constraint on UserId).
/// 
/// Customizations is stored as jsonb because:
/// - Structure varies per template (different sections, different options)
/// - Always read/written as a whole blob
/// - Deep merged on PATCH (frontend sends partial, backend merges)
/// - No need to query individual customization fields across users
/// </summary>
public sealed class Portfolio
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TemplateId { get; set; } = string.Empty;

    // Section configs, text overrides — stored as jsonb
    public string CustomizationsJson { get; set; } = "{}";

    // Which projects are featured on the portfolio
    public Guid[] SelectedProjectIds { get; set; } = [];

    // Status
    public PortfolioStatus Status { get; set; } = PortfolioStatus.Draft;
    public string? DeployedUrl { get; set; }
    public DateTimeOffset? LastDeployedAt { get; set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Template Template { get; set; } = null!;
    public List<PortfolioDeployment> Deployments { get; set; } = [];
}
