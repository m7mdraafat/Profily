using System.Security.Claims;
using Profily.Core.Interfaces;
using Profily.Core.Models;

namespace Profily.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IWideEventAccessor _wideEvent;

    public AuthService(
        IUserRepository userRepository,
        IWideEventAccessor wideEvent)
    {
        _userRepository = userRepository;
        _wideEvent = wideEvent;
    }
    
    public async Task<User> ProcessOAuthLoginAsync(ClaimsPrincipal principal, string accessToken)
    {
        var githubIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("GitHub ID claim not found.");
        
        var githubId = long.Parse(githubIdClaim);
        var username = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? throw new InvalidOperationException("GitHub username claim not found.");
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;
        var name = principal.FindFirst("urn:github:name")?.Value;

        // Check if user exists
        var existingUser = await _userRepository.GetByGitHubIdAsync(githubId);
        var isNewUser = existingUser is null;

        // Enrich wide event with auth context
        _wideEvent.WideEvent?.Set("auth.is_new_user", isNewUser);
        _wideEvent.WideEvent?.Set("auth.github_id", githubId);
        _wideEvent.WideEvent?.Set("auth.github_username", username);

        if (!isNewUser)
        {
            // Update existing user info 
            existingUser!.GitHubUsername = username;
            existingUser.Name = name;
            existingUser.Email = email;
            existingUser.AvatarUrl = avatarUrl;
            existingUser.AccessToken = accessToken;
            existingUser.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpsertAsync(existingUser);
        }

        var userId = Guid.NewGuid().ToString();
        var newUser = new User
        {
            Id = userId,
            UserId = userId, // Partition key = Id for users
            GitHubId = githubId,
            GitHubUsername = username,
            Name = name,
            Email = email,
            AvatarUrl = avatarUrl,
            AccessToken = accessToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _wideEvent.WideEvent?.Set("auth.new_user_id", userId);

        return await _userRepository.UpsertAsync(newUser);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }
}