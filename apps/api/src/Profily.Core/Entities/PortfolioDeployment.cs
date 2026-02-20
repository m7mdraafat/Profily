using Profily.Core.Enums;

namespace Profily.Core.Entities;

/// <summary>
/// Record of a deployment to GitHub Pages.
/// Named PortfolioDeployment (not Deployment) to avoid CA1724 conflict
/// with System.Deployment.Internal namespace.
/// </summary>
public sealed class PortfolioDeployment
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public string? CommitSha { get; set; }
    public string? DeployedUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public int FileCount { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation
    public Portfolio Portfolio { get; set; } = null!;
}
