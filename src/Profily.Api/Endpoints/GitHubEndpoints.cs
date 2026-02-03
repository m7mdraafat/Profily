using System.Security.Claims;
using Profily.Core.Interfaces;

namespace Profily.Api.Endpoints;

public static class GitHubEndpoints
{
    public static void MapGitHubEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/github")
            .WithTags("GitHub")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/repos", GetUserRepositories)
            .WithName("GetUserRepositories")
            .WithSummary("Get authenticated user's repositories")
            .WithDescription("Returns all public repositories owned by the authenticated user.");

        group.MapGet("/stats", GetUserStats)
            .WithName("GetUserStats")
            .WithSummary("Get user's GitHub statistics")
            .WithDescription("Returns aggregated stats including stars, forks, followers, and top languages.");

        group.MapGet("/repos/{owner}/{repo}/languages", GetRepoLanguages)
            .WithName("GetRepoLanguages")
            .WithSummary("Get repository language breakdown")
            .WithDescription("Returns the language statistics for a specific repository.");
    }

    private static async Task<IResult> GetUserRepositories(
        HttpContext context,
        IAuthService authService,
        IGitHubService gitHubService,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(
            context,
            authService);
        
        if (accessToken is null)
        {
            return Results.Unauthorized();
        }        

        var repos = await gitHubService.GetUserRepositoriesAsync(
            accessToken,
            cancellationToken);
        
        return Results.Ok(repos);
    }

    private static async Task<IResult> GetUserStats(
        HttpContext context,
        IAuthService authService,
        IGitHubService gitHubService,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(
            context,
            authService);
        
        if (accessToken is null)
        {
            return Results.Unauthorized();
        }        

        var stats = await gitHubService.GetUserStatsAsync(
            accessToken,
            cancellationToken);
        
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetRepoLanguages(
        string owner,
        string repo,
        HttpContext context,
        IGitHubService gitHubService,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(
            context,
            authService);
        
        if (accessToken is null)
        {
            return Results.Unauthorized();
        }        

        var languages = await gitHubService.GetRepositoryLanguagesAsync(
            accessToken,
            owner,
            repo,
            cancellationToken);
        
        return Results.Ok(languages);
    }

    private static async Task<string?> GetAccessTokenAsync(
        HttpContext context,
        IAuthService authService)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return null;
        }

        var user = await authService.GetUserByIdAsync(userId);
        return user?.AccessToken;
    }
}