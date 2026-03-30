using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Profily.Api.Contracts;
using Profily.Api.Extensions;
using Profily.Core.Entities;
using Profily.Core.Exceptions;
using Profily.Infrastructure.Data;

namespace Profily.Api.Endpoints;

public static class SocialLinkEndpoints
{
    public static void MapSocialLinkEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/social-links");

        group.MapGet("/", HandleListAsync);
        group.MapPost("/", HandleCreateAsync);
        group.MapPatch("/{id:guid}", HandleUpdateAsync);
        group.MapDelete("/{id:guid}", HandleDeleteAsync);
    }

    private static async Task<IResult> HandleListAsync(
        HttpContext context,
        ProfilyDbContext db)
    {
        var userId = context.GetUserId();

        var links = await db.SocialLinks
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                s.Id, s.Platform, s.Url, s.IconFilename, s.DisplayOrder
            })
            .ToListAsync(context.RequestAborted);

        return Results.Ok(links);
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateSocialLinkRequest request,
        IValidator<CreateSocialLinkRequest> validator,
        HttpContext context,
        ProfilyDbContext db)
    {
        await validator.ValidateAndThrowExAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        // Check unique platform constraint before hitting DB
        var exists = await db.SocialLinks
            .AnyAsync(s => s.UserId == userId && s.Platform == request.Platform, context.RequestAborted);

        if (exists)
            throw new ConflictException($"Social link for '{request.Platform}' already exists.");

        var link = new SocialLink
        {
            UserId = userId,
            Platform = request.Platform,
            Url = request.Url,
            IconFilename = request.IconFilename,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        db.SocialLinks.Add(link);
        await db.SaveChangesAsync(context.RequestAborted);

        return Results.Created($"/api/social-links/{link.Id}", new
        {
            link.Id, link.Platform, link.Url, link.IconFilename, link.DisplayOrder
        });
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        UpdateSocialLinkRequest request,
        IValidator<UpdateSocialLinkRequest> validator,
        HttpContext context,
        ProfilyDbContext db)
    {
        await validator.ValidateAndThrowExAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        var link = await db.SocialLinks
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, context.RequestAborted)
            ?? throw new NotFoundException("SocialLink", id);

        if (request.Url is not null) link.Url = request.Url;
        if (request.IconFilename is not null) link.IconFilename = request.IconFilename;
        if (request.DisplayOrder.HasValue) link.DisplayOrder = request.DisplayOrder.Value;

        await db.SaveChangesAsync(context.RequestAborted);

        return Results.Ok(new
        {
            link.Id, link.Platform, link.Url, link.IconFilename, link.DisplayOrder
        });
    }

    private static async Task<IResult> HandleDeleteAsync(
        Guid id,
        HttpContext context,
        ProfilyDbContext db)
    {
        var userId = context.GetUserId();

        var deleted = await db.SocialLinks
            .Where(s => s.Id == id && s.UserId == userId)
            .ExecuteDeleteAsync(context.RequestAborted);

        if (deleted == 0)
            throw new NotFoundException("SocialLink", id);

        return Results.NoContent();
    }
}