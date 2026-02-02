using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models;

namespace Profily.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ICosmosDbService cosmosDbService,
        ILogger<AuthService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }
    
    public async Task<User> ProcessOAuthLoginAsync(ClaimsPrincipal principal, string accessToken)
    {
        var githubIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("GitHub ID claim not found.");
        
        var githubId = int.Parse(githubIdClaim);
        var username = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? throw new InvalidOperationException("GitHub username claim not found.");
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;
        var name = principal.FindFirst("urn:github:name")?.Value;

        // Check if user exists
        var existingUser = await _cosmosDbService.GetUserByGitHubIdAsync(githubId);

        if (existingUser is not null)
        {
            _logger.LogInformation("Existing user login: {Username} (ID: {UserId})", username, existingUser.Id);

            // Update user info 
            existingUser.GitHubUsername = username;
            existingUser.Name = name;
            existingUser.Email = email;
            existingUser.AvatarUrl = avatarUrl;
            existingUser.AccessToken = accessToken;
            existingUser.UpdatedAt = DateTime.UtcNow;

            return await _cosmosDbService.UpsertUserAsync(existingUser);
        }

        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            GitHubId = githubId,
            GitHubUsername = username,
            Name = name,
            Email = email,
            AvatarUrl = avatarUrl,
            AccessToken = accessToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("New user registered: {Username} (ID: {UserId})", username, newUser.Id);

        return await _cosmosDbService.UpsertUserAsync(newUser);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _cosmosDbService.GetUserByIdAsync(userId);
    }
}