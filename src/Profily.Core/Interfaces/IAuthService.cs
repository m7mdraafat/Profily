using System.Security.Claims;
using Profily.Core.Models;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for handling authentication-related operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates or updates a user from OAuth claims and persists to database.
    /// </summary>
    Task<User> ProcessOAuthLoginAsync(ClaimsPrincipal principal, string accessToken);

    /// <summary>
    /// Gets user by their internal ID.
    /// </summary>
    Task<User?> GetUserByIdAsync(string userId);
}