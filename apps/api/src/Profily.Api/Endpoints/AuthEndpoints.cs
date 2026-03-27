using Microsoft.EntityFrameworkCore;
using Profily.Api.Extensions;
using Profily.Core.Entities;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;

using GitHubUserProfile = Profily.Core.Interfaces.GitHubUserProfile;

namespace Profily.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapGet("/github", HandleGitHubRedirect);
        group.MapGet("/callback", HandleCallbackAsync);
        group.MapGet("/me", HandleMe);
        group.MapPost("/logout", HandleLogoutAsync);
    }

    private static IResult HandleGitHubRedirect(
        IGitHubAuthService github,
        HttpContext context)
    {
        // Generate random state for CSRF protection on OAuth flow
        var state = Guid.NewGuid().ToString("N");

        // Store state in short-lived cookie to verify on callback.
        context.Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax, // same-site redirect,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/api/auth"
        });
        
        var url = github.GetAuthorizationUrl(state);
        return Results.Redirect(url);
    }

    private static async Task<IResult> HandleCallbackAsync(
        string code,
        string state,
        IGitHubAuthService github,
        ITokenEncryptionService encryption,
        ISessionService sessions,
        ProfilyDbContext dbContext,
        HttpContext context,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        // Verify OAuth state
        if (!context.Request.Cookies.TryGetValue("oauth_state", out var savedState) || savedState != state)
        {
            logger.LogWarning("OAuth state mismatch");
            return Results.BadRequest("Invalid OAuth state");
        }

        // Clear state cookie
        context.Response.Cookies.Delete("oauth_state", new CookieOptions { Path = "/api/auth" });

        // Exchange code for token
        string accessToken;
        GitHubUserProfile profile;
        try
        {
            accessToken = await github.ExchangeCodeForTokenAsync(code, context.RequestAborted);
            profile = await github.GetUserProfileAsync(accessToken, context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GitHub OAuth exchange failed");
            var frontendErrorUrl = (configuration["FrontendUrl"] ?? "http://localhost:5173") + "/login?error=github";
            return Results.Redirect(frontendErrorUrl);
        }

        // Upsert user
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.GitHubId == profile.Id, context.RequestAborted);

        if (user is null)
        {
            user = new User
            {
                GitHubId = profile.Id,
                Username = profile.Login,
                DisplayName = profile.Name,
                AvatarUrl = profile.AvatarUrl,
                Bio = profile.Bio,
                Location = profile.Location,
                Company = profile.Company,
                Email = profile.Email,
                GitHubUrl = profile.HtmlUrl,
                GitHubTokenEncrypted = encryption.Encrypt(accessToken),
                ReposCount = profile.PublicRepos,
                FollowersCount = profile.Followers,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(user);
            logger.LogInformation("New user registered: {Username}", profile.Login);
        }
        else
        {
            // Update profile data and token on every login
            user.Username = profile.Login;
            user.DisplayName = profile.Name;
            user.AvatarUrl = profile.AvatarUrl;
            user.Bio = profile.Bio;
            user.Location = profile.Location;
            user.Company = profile.Company;
            user.Email = profile.Email;
            user.GitHubUrl = profile.HtmlUrl;
            user.GitHubTokenEncrypted = encryption.Encrypt(accessToken);
            user.ReposCount = profile.PublicRepos;
            user.FollowersCount = profile.Followers;
            user.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("User logged in: {Username}", profile.Login);
        }

        await dbContext.SaveChangesAsync(context.RequestAborted);

        // Clean old sessions for this user, then create new
        await sessions.DeleteByUserAsync(user.Id, context.RequestAborted);
        var session = await sessions.CreateAsync(user.Id, context.RequestAborted);

        // Set session cookie
        context.Response.Cookies.Append("session", session.Id.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // Cross-Origin (Cloudflare Pages -> Azure)
            MaxAge = TimeSpan.FromDays(7),
            Path = "/api"
        }); 

        // Redirect to frontend after successful login
        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";
        return Results.Redirect(frontendUrl);
    }

    private static IResult HandleMe(HttpContext context)
    {
        var user = context.GetUser();

        return Results.Ok(new
        {
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            user.Location,
            user.Company,
            user.Email,
            user.GitHubUrl,
            user.ReposCount,
            user.FollowersCount,
            user.ContributionsThisYear,
            Plan = user.Plan.ToString().ToLowerInvariant(),
            user.PlanExpiresAt,
            user.LastSyncedAt,
            user.CreatedAt
        });
    }

    private static async Task<IResult> HandleLogoutAsync(
        HttpContext context,
        ISessionService sessions,
        ILogger<Program> logger)
    {
        var username = context.GetUser().Username;

        if (context.Request.Cookies.TryGetValue("session", out var sessionIdStr) && Guid.TryParse(sessionIdStr, out var sessionId))
        {
            await sessions.DeleteAsync(sessionId, context.RequestAborted);
        }

        context.Response.Cookies.Delete("session", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api"
        });

        logger.LogInformation("User logged out: {Username}", username);
        return Results.Ok();
    }
}