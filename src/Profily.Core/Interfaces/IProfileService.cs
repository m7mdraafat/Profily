using Profily.Core.Models.Profile;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for managing user profile configurations and deploy history.
/// All operations are scoped to a specific user.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Get the user's current profile configuration.
    /// Returns null if user hasn't started building a profile yet.
    /// </summary>
    /// <param name="userId">Internal user ID (GUID)</param>
    Task<ProfileConfig?> GetUserConfigAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Save or update the user's profile configuration.
    /// Creates a new config if none exists, updates if it does.
    /// Automatically sets UpdatedAt timestamp.
    /// </summary>
    /// <param name="config">Configuration to save (must have UserId set)</param>
    /// <returns>The saved configuration with updated timestamps</returns>
    Task<ProfileConfig> SaveUserConfigAsync(ProfileConfig config, CancellationToken ct = default);

    /// <summary>
    /// Create a new profile configuration from a template.
    /// Copies the template's sections, styles, and theme to a new user config.
    /// Replaces any existing config for the user.
    /// </summary>
    /// <param name="userId">User to create config for</param>
    /// <param name="templateSlug">Template to copy from</param>
    /// <returns>The newly created configuration</returns>
    /// <exception cref="ArgumentException">If template slug not found</exception>
    Task<ProfileConfig> CreateFromTemplateAsync(
        string userId, 
        string templateSlug, 
        CancellationToken ct = default);

    /// <summary>
    /// Delete the user's profile configuration.
    /// Silently succeeds if no config exists.
    /// Does NOT delete deploy history (audit trail preserved).
    /// </summary>
    Task DeleteUserConfigAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get the user's deploy history, most recent first.
    /// </summary>
    /// <param name="userId">User to get history for</param>
    /// <param name="limit">Maximum number of records to return (default 10, max 50)</param>
    Task<List<DeployHistory>> GetDeployHistoryAsync(
        string userId, 
        int limit = 10, 
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific deploy record by ID.
    /// UserId is required for partition key efficiency.
    /// </summary>
    /// <returns>Deploy record if found and belongs to user, null otherwise</returns>
    Task<DeployHistory?> GetDeployByIdAsync(
        string deployId, 
        string userId, 
        CancellationToken ct = default);

    /// <summary>
    /// Record a deploy operation (success or failure).
    /// Creates an immutable audit record with config snapshot.
    /// Also updates the ProfileConfig's LastDeployedAt if successful.
    /// </summary>
    /// <param name="userId">User who deployed</param>
    /// <param name="config">Config at time of deploy (will be snapshotted)</param>
    /// <param name="result">Outcome of the deploy operation</param>
    /// <returns>The created deploy history record</returns>
    Task<DeployHistory> RecordDeployAsync(
        string userId, 
        ProfileConfig config, 
        DeployResult result, 
        CancellationToken ct = default);
}