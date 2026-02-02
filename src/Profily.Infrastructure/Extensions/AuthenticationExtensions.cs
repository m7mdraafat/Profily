using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profily.Core.Options;

namespace Profily.Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    public const string GitHubScheme = "GitHub";

    public static IServiceCollection AddGitHubAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var gitHubOptions = configuration
            .GetSection(GitHubOptions.SectionName)
            .Get<GitHubOptions>()
            ?? throw new InvalidOperationException($"{GitHubOptions.SectionName} configuration section is missing or invalid.");

        services.Configure<GitHubOptions>(
            configuration.GetSection(GitHubOptions.SectionName));
        
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GitHubScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = "Profily.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax; // Required for OAuth redirects
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            // Return 401 instead of redirect for API calls
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            };
        })
        .AddGitHub(options =>
        {
            options.ClientId = gitHubOptions.ClientId;
            options.ClientSecret = gitHubOptions.ClientSecret;
            options.CallbackPath = new PathString("/auth/github/callback");

            // Request scopes
            options.Scope.Clear();
            foreach (var scope in gitHubOptions.Scopes)
            {
                options.Scope.Add(scope);
            }

            // Save tokens so we can use GitHub API
            options.SaveTokens = true;

            // Map GitHub claims to standard claims
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
        });

        services.AddAuthentication();
        
        return services;
    }
}