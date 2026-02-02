using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Profily.Core.Interfaces;
using Profily.Core.Models.Auth;
using Profily.Infrastructure.Extensions;

namespace Profily.Api.Endpoints;

/// <summary>
/// Authentication-related API endpoints.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();
        
        group.MapGet("/github", InitialGitHubLogin)
            .WithName("GitHubLogin")
            .WithSummary("Initiates the GitHub OAuth login process.")
            .WithDescription("Redirects the user to GitHub's OAuth authorization page.");
        
        group.MapGet("/github/callback", (Delegate)GitHubOAuthCallback)
            .WithName("GitHubOAuthCallback")
            .WithSummary("Handles the GitHub OAuth callback.")
            .WithDescription("Processes the OAuth callback from GitHub and authenticates the user.");
        
        group.MapGet("/me", (Delegate)GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Gets current authenticated user")
            .WithDescription("Returns the currently authenticated user's information. Returns 401 if not authenticated.")
            .RequireAuthorization();

        group.MapPost("/logout", (Delegate)Logout)
            .WithName("Logout")
            .WithSummary("Logs out the current user.")
            .WithDescription("Invalidates the current user's session or token.")
            .RequireAuthorization();
    }

    /// <summary>
    /// Initiates the GitHub OAuth login process by redirecting to GitHub's authorization page.
    /// </summary>
    private static IResult InitialGitHubLogin(
        HttpContext context,
        string? returnUrl = null)
    {
        // Validate return URL to prevent open redirect attacks
        var validateReturnUrl = ValidateReturnUrl(returnUrl) ?? "/";

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/github/callback?returnUrl={Uri.EscapeDataString(validateReturnUrl)}",
            IsPersistent = true
        };

        return Results.Challenge(
            properties, [AuthenticationExtensions.GitHubScheme]);
    }

    /// <summary>
    /// Handles the OAuth callback from GitHub.
    /// </summary>
    private static async Task<IResult> GitHubOAuthCallback(
        HttpContext context,
        IAuthService authService,
        ILogger<Program> logger,
        string? returnUrl = null)
    {
        // Authenticate using the external scheme to get GitHub claims
        var authenticateResult = await context.AuthenticateAsync(
            AuthenticationExtensions.GitHubScheme);
        
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            logger.LogWarning("GitHub authentication failed: {Failure}", authenticateResult.Failure?.Message);
            return Results.Problem(
                title: "Authentication Failed",
                detail: "GitHub authentication was not successful. Please try again.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // Get the access token
        var accessToken = authenticateResult.Properties.GetTokenValue("access_token")
            ?? throw new InvalidOperationException("Access token not found in authentication result.");

        // Process login
        var user = await authService.ProcessOAuthLoginAsync(
            authenticateResult.Principal, accessToken);
        
        // Create claims principal for cookie authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.GitHubUsername),
            new Claim("github_id", user.GitHubId.ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            claims.Add(new Claim("avatar_url", user.AvatarUrl));
        }

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(claimsIdentity);

        // Sign in the user with cookie authentication
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                AllowRefresh = true
            });
        
        // Redirect to the original return URL or home
<<<<<<< HEAD
        var frontendBaseUrl = "http://localhost:5183";
        var validReturnUrl = ValidateReturnUrl(returnUrl);
        
        // Convert relative URLs to absolute frontend URLs
        string redirectUrl;
        if (validReturnUrl is null)
        {
            redirectUrl = frontendBaseUrl;
        }
        else if (validReturnUrl.StartsWith('/'))
        {
            redirectUrl = $"{frontendBaseUrl}{validReturnUrl}";
        }
        else
        {
            redirectUrl = validReturnUrl;
        }
        
        return Results.Redirect(redirectUrl);
=======
        var validReturnUrl = ValidateReturnUrl(returnUrl) ?? "http://localhost:5183/";
        return Results.Redirect(validReturnUrl);
>>>>>>> a5348cb8cf9e3913608c9153275d45be2e5b176b
    }


    /// <summary>
    /// Gets the currently authenticated user's information.
    /// </summary>
    private static async Task<IResult> GetCurrentUser(
        HttpContext context,
        IAuthService authService)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Json(
                new AuthResponse { IsAuthenticated = false },
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        var user = await authService.GetUserByIdAsync(userId);
        if (user is null)
        {
            return Results.Json(
                new AuthResponse { IsAuthenticated = false },
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        return Results.Ok(new AuthResponse
        {
            IsAuthenticated = true,
            User = new UserInfoResponse
            {
                Id = user.Id,
                GitHubUsername = user.GitHubUsername,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            }
        });  
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    private static async Task<IResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        return Results.Ok(new { Message = "successfully logged out" });
    }

    /// <summary>
    /// Validates return URL to prevent open redirect attacks.
    /// Only allows relative URLs or URLs to known safe origins.
    /// </summary>
    private static string? ValidateReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return null;
        }

        // Allow relative URLs
        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return returnUrl;
        }

        // Allow known safe origins (frontend URLs)
        var safeOrigins = new[]
        {
            "http://localhost:5183",
            "https://localhost:5183",
            // Add production frontend URL here
        };

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var origin = $"{uri.Scheme}://{uri.Authority}";
            if (safeOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                return returnUrl;
            }
        }

        return null;
    }
}