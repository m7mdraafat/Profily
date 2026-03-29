using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Profily.Core.Entities;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;

namespace Profily.Infrastructure.Services;

public sealed class ProjectSyncService : IProjectSyncService
{
    private readonly ProfilyDbContext _dbContext;
    private readonly IGitHubApiService _githubApi;
    private readonly ILogger<ProjectSyncService> _logger;

    public ProjectSyncService(
        ProfilyDbContext dbContext,
        IGitHubApiService githubApi,
        ILogger<ProjectSyncService> logger)
    {
        _dbContext = dbContext;
        _githubApi = githubApi;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(Guid userId, string accessToken, CancellationToken ct = default)
    {
        var githubRepos = await _githubApi.GetRepositoriesAsync(accessToken, ct);
        var existingProjects = await _dbContext.Projects
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.GitHubRepoId, ct);
        
        var newCount = 0;
        var updatedCount = 0;

        foreach (var repo in githubRepos)
        {
            if (existingProjects.TryGetValue(repo.Id, out var existing))
            {
                // Update existing project
                existing.Name = repo.Name;
                existing.Description = repo.Description;
                existing.Language = repo.Language;
                existing.Topics = repo.Topics;
                existing.Stars = repo.StargazersCount;
                existing.Forks = repo.ForksCount;
                existing.IsFork = repo.Fork;
                existing.HtmlUrl = repo.HtmlUrl;
                existing.HomepageUrl = repo.Homepage;
                existing.LastPushedAt = repo.PushedAt;
                existing.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
            else
            {
                // Insert new project
                _dbContext.Projects.Add(new Project
                {
                    UserId = userId,
                    GitHubRepoId = repo.Id,
                    Name = repo.Name,
                    Description = repo.Description,
                    Language = repo.Language,
                    Topics = repo.Topics,
                    Stars = repo.StargazersCount,
                    Forks = repo.ForksCount,
                    IsFork = repo.Fork,
                    HtmlUrl = repo.HtmlUrl,
                    HomepageUrl = repo.Homepage,
                    LastPushedAt = repo.PushedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                newCount++;
            }
        }

        // Update user stats
        var user = await _dbContext.Users.FirstAsync(u => u.Id == userId, ct);
        user.ReposCount = githubRepos.Count;
        user.LastSyncedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Repo sync for {Username}: {NewCount} new, {UpdatedCount} updated, {TotalCount} total",
            user.Username,
            newCount,
            updatedCount,
            githubRepos.Count
        );

        return new SyncResult
        {
            NewRepos = newCount,
            UpdatedRepos = updatedCount,
            TotalRepos = githubRepos.Count
        };
    }
}