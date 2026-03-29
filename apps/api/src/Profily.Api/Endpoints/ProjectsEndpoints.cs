using Microsoft.EntityFrameworkCore;
using Profily.Api.Extensions;
using Profily.Core.Exceptions;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;

namespace Profily.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects");

        group.MapPost("/sync", HandleSyncAsync);
        group.MapGet("/", HandleListAsync);
        group.MapPatch("/{id:guid}", HandleUpdateAsync);
    }

    private static async Task<IResult> HandleSyncAsync(
        IProjectSyncService syncService,
        ITokenEncryptionService encryption,
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        var user = context.GetUser();

        var accessToken = encryption.Decrypt(user.GitHubTokenEncrypted);
        var result = await syncService.SyncAsync(user.Id, accessToken, context.RequestAborted);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleListAsync(
        HttpContext context,
        ProfilyDbContext dbContext,
        bool? isEnabled,
        int page = 1,
        int pageSize = 20)
    {
        var userId = context.GetUserId();

        var query = dbContext.Projects
            .Where(p => p.UserId == userId)
            .AsQueryable();

        if (isEnabled.HasValue)
        {
            query = query.Where(p => p.IsEnabled == isEnabled.Value);
        }

        var totalCount = await query.CountAsync(context.RequestAborted);

        var projects = await query
            .OrderByDescending(p => p.IsEnabled)
            .ThenBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Stars)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.CustomDescription,
                p.Language,
                p.Topics,
                p.Stars,
                p.Forks,
                p.IsFork,
                p.HtmlUrl,
                p.HomepageUrl,
                p.IsEnabled,
                p.DisplayOrder,
                p.LastPushedAt
            })
            .ToListAsync(context.RequestAborted);

        return Results.Ok(new
        {
            data = projects,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        HttpContext context,
        ProfilyDbContext dbContext)
    {
        var userId = context.GetUserId();

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, context.RequestAborted)
            ?? throw new NotFoundException("Project", id);

        if (request.IsEnabled is true && !project.IsEnabled)
        {
            var enabledCount = await dbContext.Projects
                .CountAsync(p => p.UserId == userId && p.IsEnabled, context.RequestAborted);
            
            if (enabledCount >= 10)
            {
                throw new ConflictException("Maximum of 10 projects can be enabled.");
            }
        }    

        if (request.IsEnabled.HasValue)
        {
            project.IsEnabled = request.IsEnabled.Value;
        }        

        if (request.DisplayOrder.HasValue)
        {
            project.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.CustomDescription is not null)
        {
            project.CustomDescription = request.CustomDescription;
        }

        project.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.RequestAborted);

        return Results.Ok(new
        {
            project.Id,
            project.Name,
            project.Description,
            project.CustomDescription,
            project.Language,
            project.Topics,
            project.Stars,
            project.Forks,
            project.IsFork,
            project.HtmlUrl,
            project.HomepageUrl,
            project.IsEnabled,
            project.DisplayOrder,
            project.LastPushedAt
        });
    }
}

public sealed class UpdateProjectRequest
{
    public bool? IsEnabled { get; set; }
    public int? DisplayOrder { get; set; }
    public string? CustomDescription { get; set; }
}