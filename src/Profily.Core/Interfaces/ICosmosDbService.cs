using Profily.Core.Models;

namespace Profily.Core.Interfaces;

/// <summary>
/// Interface for Cosmos DB service operations
/// </summary>
// ICosmosDbService.cs
public interface ICosmosDbService
{
    Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByGitHubIdAsync(long gitHubId, CancellationToken cancellationToken = default);
    Task<User> UpsertUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}