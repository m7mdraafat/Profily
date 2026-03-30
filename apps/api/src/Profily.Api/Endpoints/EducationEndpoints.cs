using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Profily.Api.Contracts;
using Profily.Api.Extensions;
using Profily.Core.Entities;
using Profily.Core.Exceptions;
using Profily.Infrastructure.Data;

namespace Profily.Api.Endpoints;

public static class EducationEndpoints
{
    public static void MapEducationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/educations");

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

        var educations = await db.Educations
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.DisplayOrder)
            .Select(e => new
            {
                e.Id, e.Degree, e.School, e.StartDate, e.EndDate,
                e.Description, e.DisplayOrder
            })
            .ToListAsync(context.RequestAborted);

        return Results.Ok(educations);
    }

    private static async Task<IResult> HandleCreateAsync(
        CreateEducationRequest request,
        IValidator<CreateEducationRequest> validator,
        HttpContext context,
        ProfilyDbContext db)
    {
        await validator.ValidateAndThrowExAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        var education = new Education
        {
            UserId = userId,
            Degree = request.Degree,
            School = request.School,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Educations.Add(education);
        await db.SaveChangesAsync(context.RequestAborted);

        return Results.Created($"/api/educations/{education.Id}", new
        {
            education.Id, education.Degree, education.School, education.StartDate,
            education.EndDate, education.Description, education.DisplayOrder
        });
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        UpdateEducationRequest request,
        IValidator<UpdateEducationRequest> validator,
        HttpContext context,
        ProfilyDbContext db)
    {
        await validator.ValidateAndThrowExAsync(request, context.RequestAborted);

        var userId = context.GetUserId();

        var education = await db.Educations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, context.RequestAborted)
            ?? throw new NotFoundException("Education", id);

        if (request.Degree is not null) education.Degree = request.Degree;
        if (request.School is not null) education.School = request.School;
        if (request.StartDate.HasValue) education.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) education.EndDate = request.EndDate.Value;
        if (request.Description is not null) education.Description = request.Description;
        if (request.DisplayOrder.HasValue) education.DisplayOrder = request.DisplayOrder.Value;

        education.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);

        return Results.Ok(new
        {
            education.Id, education.Degree, education.School, education.StartDate,
            education.EndDate, education.Description, education.DisplayOrder
        });
    }

    private static async Task<IResult> HandleDeleteAsync(
        Guid id,
        HttpContext context,
        ProfilyDbContext db)
    {
        var userId = context.GetUserId();

        var deleted = await db.Educations
            .Where(e => e.Id == id && e.UserId == userId)
            .ExecuteDeleteAsync(context.RequestAborted);

        if (deleted == 0)
            throw new NotFoundException("Education", id);

        return Results.NoContent();
    }
}