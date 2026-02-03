using Profily.Core.Models;

namespace Profily.Core.Interfaces;

/// <summary>
/// Repository for User-specific operations.
/// Provides domain-specific queries beyond generic CRUD.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get a user by their internal ID.
    /// </summary>
    /// <param name="userId">Internal user ID (GUID)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get a user by their GitHub numeric ID.
    /// Used during OAuth login to find existing users.
    /// </summary>
    /// <param name="gitHubId">GitHub's numeric user ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByGitHubIdAsync(long gitHubId, CancellationToken ct = default);

    /// <summary>
    /// Create or update a user.
    /// Automatically updates the UpdatedAt timestamp.
    /// </summary>
    /// <param name="user">User to upsert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The upserted user</returns>
    Task<User> UpsertAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Delete a user and all associated data.
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteAsync(string userId, CancellationToken ct = default);
}
