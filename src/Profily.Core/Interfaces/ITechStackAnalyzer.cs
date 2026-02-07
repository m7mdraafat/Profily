using Profily.Core.Models.TechStack;

namespace Profily.Core.Interfaces;

/// <summary>
/// Analyzes a user's GitHub repositories to detect their technology stack (languages, frameworks, tools).
/// Results are persisted to Cosmos DB with in-memory cache optmization.
/// </summary>
public interface ITechStackAnalyzer
{
    /// <summary>
    /// Get the user's tech stack profile. Reads from:
    /// 1. Memory cache (6h TTL)
    /// 2. Cosmos DB (if cache miss)
    /// 3. Fresh analysis (if stale or missing in DB)
    /// </summary>
    Task<TechStackProfile> GetTechStackAsync(
        string userId,
        string accessToken,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Force re-analysis bypassing cache and DB checks. Used for manual refresh or if we detect stale data.
    /// </summary>
    Task<TechStackProfile> RefreshTechStackAsync(
        string userId,
        string accessToken,
        CancellationToken cancellationToken = default);
}