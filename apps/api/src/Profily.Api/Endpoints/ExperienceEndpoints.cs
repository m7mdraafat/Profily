using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Profily.Api.Contracts;
using Profily.Api.Extensions;
using Profily.Core.Entities;
using Profily.Core.Exceptions;
using Profily.Infrastructure.Data;

namespace Profily.Api.Endpoints;

public static class ExperienceEndpoints
{
    public static void MapExperienceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/experiences");

        group.MapGet("/", HandleListAsync);
        group.MapPost("/", HandleCreateAsync);
        group.MapPatch("/{id:guid}", HandleUpdateAsync);
        group.MapDelete("/{id:guid}", HandleDeleteAsync);
    }

    private static async Task<IResult> HandleListAsync(
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        var userId = context.GetUserId();

        var experiences = await dbContext.Experiences
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.DisplayOrder)
            .Select(e => new
            {
                e.Id, e.Title, e.Company, e.StartDate, e.EndDate,
                e.IsCurrent, e.Description, e.DisplayOrder
            })
            .ToListAsync(context.RequestAborted);

        return Results.Ok(experiences);
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateExperienceRequest request,
        IValidator<CreateExperienceRequest> validator,
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        await validator.ValidateAndThrowAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        var experience = new Experience
        {
            UserId = userId,
            Title = request.Title,
            Company = request.Company,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Experiences.Add(experience);
        await dbContext.SaveChangesAsync(context.RequestAborted);

        return Results.Created($"/api/experiences/{experience.Id}",  new
        {
            experience.Id, experience.Title, experience.Company, experience.StartDate,
            experience.EndDate, experience.IsCurrent, experience.Description, experience.DisplayOrder
        });
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        UpdateExperienceRequest request,
        IValidator<UpdateExperienceRequest> validator,
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        await validator.ValidateAndThrowAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        var experience = await dbContext.Experiences
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, context.RequestAborted)
            ?? throw new NotFoundException("Experience", id);

        if (request.Title is not null) experience.Title = request.Title;
        if (request.Company is not null) experience.Company = request.Company;
        if (request.StartDate.HasValue) experience.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) experience.EndDate = request.EndDate.Value;
        if (request.IsCurrent.HasValue) experience.IsCurrent = request.IsCurrent.Value;
        if (request.Description is not null) experience.Description = request.Description;
        if (request.DisplayOrder.HasValue) experience.DisplayOrder = request.DisplayOrder.Value;

        experience.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.RequestAborted);

        return Results.Ok(new
        {
            experience.Id, experience.Title, experience.Company, experience.StartDate,
            experience.EndDate, experience.IsCurrent, experience.Description, experience.DisplayOrder
        });
    }

    private static async Task<IResult> HandleDeleteAsync(
        Guid id,
        HttpContext context,
        ProfilyDbContext db)
    {
        var userId = context.GetUserId();

        var deleted = await db.Experiences
            .Where(e => e.Id == id && e.UserId == userId)
            .ExecuteDeleteAsync(context.RequestAborted);

        if (deleted == 0)
            throw new NotFoundException("Experience", id);

        return Results.NoContent();
    }
}