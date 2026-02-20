namespace Profily.Core.Enums;

/// <summary>
/// Enum stored as varchar in PostgreSQL via EF Core HasConversion.
/// Provides compile-time safety over string constants.
/// </summary>
public enum PortfolioStatus
{
    Draft,
    Deployed
}
