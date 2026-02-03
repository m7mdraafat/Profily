namespace Profily.Infrastructure.Data.Seeding;

/// <summary>
/// Interface for seeding data to Cosmos DB.
/// Seeders run in order based on Priority (lower runs first).
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Priority determines the order of execution. Lower values run first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Seeds data to the database. Implementations should be idempotent.
    /// </summary>
    Task SeedAsync(CancellationToken ct = default);
}