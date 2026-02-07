using System.Security.Claims;
using Profily.Core.Interfaces;

namespace Profily.Api.Endpoints;

public static class TechStackEndpoints
{
    public static void MapTechStackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/techstack")
            .WithTags("TechStack")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetTechStack)
            .WithName("GetTechStack")
            .WithSummary("Get detected tech stack")
            .WithDescription("Returns the user's detected technologies from cache, DB, or fresh analysis.");

        group.MapPost("/refresh", RefreshTechStack)
            .WithName("RefreshTechStack")
            .WithSummary("Force re-analyze tech stack")
            .WithDescription("Bypasses cache and staleness to run fresh analysis.");
    }

    private static async Task<IResult> GetTechStack(
        HttpContext context,
        IAuthService authService,
        ITechStackAnalyzer analyzer,
        CancellationToken ct)
    {
        var (userId, accessToken) = await ResolveUserAsync(context, authService);
        if (userId is null || accessToken is null)
            return Results.Unauthorized();

        var profile = await analyzer.GetTechStackAsync(userId, accessToken, ct);
        return Results.Ok(profile);
    }

    private static async Task<IResult> RefreshTechStack(
        HttpContext context,
        IAuthService authService,
        ITechStackAnalyzer analyzer,
        CancellationToken ct)
    {
        var (userId, accessToken) = await ResolveUserAsync(context, authService);
        if (userId is null || accessToken is null)
            return Results.Unauthorized();

        var profile = await analyzer.RefreshTechStackAsync(userId, accessToken, ct);
        return Results.Ok(profile);
    }

    private static async Task<(string? UserId, string? AccessToken)> ResolveUserAsync(
        HttpContext context, IAuthService authService)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return (null, null);

        var user = await authService.GetUserByIdAsync(userId);
        return (userId, user?.AccessToken);
    }
}