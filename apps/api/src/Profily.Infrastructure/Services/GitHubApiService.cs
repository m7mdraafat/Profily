using Octokit;
using Profily.Core.Interfaces;

namespace Profily.Infrastructure.Services;

public sealed class GitHubApiService : IGitHubApiService
{
    
    public async Task<List<GitHubRepository>> GetRepositoriesAsync(string accessToken, CancellationToken ct = default)
    {
        var client = new GitHubClient(new ProductHeaderValue("Profily"))
        {
            Credentials = new Credentials(accessToken)
        };

        var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Type = RepositoryType.Owner,
            Sort = RepositorySort.Pushed,
            Direction = SortDirection.Descending,
        });

        return repos.Select(r => new GitHubRepository
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Language = r.Language,
            Topics = r.Topics?.ToList() ?? [],
            StargazersCount = r.StargazersCount,
            ForksCount = r.ForksCount,
            Fork = r.Fork,
            HtmlUrl = r.HtmlUrl,
            Homepage = r.Homepage,
            PushedAt = r.PushedAt?.UtcDateTime
        }).ToList();
    }
}