namespace Profily.Core.Entities;

/// <summary>
/// Business analytics event — stored in PostgreSQL for product insights.
/// Separate from logs (Application Insights) because:
/// - Logs are for debugging (retained 90 days)
/// - Domain events are for business analytics (retained forever)
/// - "Which templates are popular?" "What skills do users remove?" "Conversion rate?"
/// </summary>
public sealed class DomainEvent
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "portfolio.deployed", "skills.edited"
    public Guid? UserId { get; set; }
    public string DataJson { get; set; } = "{}"; // jsonb — event-specific payload
    public DateTimeOffset CreatedAt { get; set; }
}
