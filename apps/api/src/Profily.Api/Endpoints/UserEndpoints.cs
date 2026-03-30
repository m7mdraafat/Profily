using FluentValidation;
using Profily.Api.Contracts;
using Profily.Api.Extensions;
using Profily.Infrastructure.Data;

namespace Profily.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapPatch("/api/users/me", HandleUpdateAsync);
    }

    private static async Task<IResult> HandleUpdateAsync(
        UpdateUserRequest request,
        IValidator<UpdateUserRequest> validator,
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        await validator.ValidateAndThrowAsync(request, context.RequestAborted);

        var user = context.GetUser();

        if (request.DisplayName is not null) user.DisplayName = request.DisplayName;
        if (request.Bio is not null) user.Bio = request.Bio;
        if (request.Location is not null) user.Location = request.Location;
        if (request.Company is not null) user.Company = request.Company;
        if (request.Email is not null) user.Email = request.Email;

        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.RequestAborted);

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

}