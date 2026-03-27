using Profily.Core.Enums;

namespace Profily.Core.Entities;

public sealed class Portfolio
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public PortfolioStatus Status { get; set; } = PortfolioStatus.Draft;
    public string Customizations { get; set; } = "{}";
    public string? DeployedUrl { get; set; }
    public string? GitHubPagesUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
