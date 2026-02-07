using Microsoft.Extensions.Caching.Memory;
using Octokit;
using Profily.Core.Interfaces;
using Profily.Core.Models.GitHub;

namespace Profily.Infrastructure.GitHub;

public class GitHubService : IGitHubService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IWideEventAccessor _wideEvent;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public GitHubService(
        IMemoryCache memoryCache,
        IWideEventAccessor wideEvent)
    {
        _memoryCache = memoryCache;
        _wideEvent = wideEvent;
    }

    public async Task<List<GitHubRepository>> GetUserRepositoriesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(accessToken);
        var user = await client.User.Current();
        var cacheKey = $"repo_{accessToken.GetHashCode()}";

        if (_memoryCache.TryGetValue(cacheKey, out List<GitHubRepository>? cached) && 
            cached is not null)
        {
            _wideEvent.WideEvent?.Set("github.repos.cache_hit", true);
            return cached;
        }

        _wideEvent.WideEvent?.Set("github.repos.cache_hit", false);
        _wideEvent.WideEvent?.Set("github.repos.username", user.Login);

        var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Type = RepositoryType.Owner,
            Sort = RepositorySort.Updated
        });

        var result = repos
            .Where(r => !r.Private && !r.Fork)
            .Select(MapToGitHubRepository)
            .ToList();

        _wideEvent.WideEvent?.Set("github.repos.count", result.Count);
        _memoryCache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    public async Task<GitHubStats> GetUserStatsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(accessToken);
        var user = await client.User.Current();
        var cacheKey = $"stats_{accessToken.GetHashCode()}";

        if (_memoryCache.TryGetValue(cacheKey, out GitHubStats? cached) && 
            cached is not null)
        {
            _wideEvent.WideEvent?.Set("github.stats.cache_hit", true);
            return cached;
        }

        _wideEvent.WideEvent?.Set("github.stats.cache_hit", false);
        _wideEvent.WideEvent?.Set("github.stats.username", user.Login);

        // Get all repositories to aggregate stats
        var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Type = RepositoryType.Owner,
        });

        var ownedRepos = repos.Where(r => !r.Fork && !r.Private).ToList();

        // Aggregate language stats for top repos (limit to avoid rate limits)
        var languageBytes = new Dictionary<string, long>();
        var topRepos = ownedRepos
            .OrderByDescending(r => r.StargazersCount)
            .Take(10);
        
        var languageReposFetched = 0;
        foreach (var repo in topRepos)
        {
            try
            {
                var languages = await client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
                languageReposFetched++;
                foreach (var language in languages)
                {
                    if (languageBytes.ContainsKey(language.Name))
                    {
                        languageBytes[language.Name] += language.NumberOfBytes;
                    }
                    else
                    {
                        languageBytes[language.Name] = language.NumberOfBytes;
                    }
                }
            }
            catch (RateLimitExceededException ex)
            {
                _wideEvent.WideEvent?.Set("github.rate_limit_exceeded", true);
                _wideEvent.WideEvent?.Set("github.rate_limit_reset", ex.Reset.ToString("o"));
                break;  // Stop making requests
            }
        }

        _wideEvent.WideEvent?.Set("github.stats.language_repos_fetched", languageReposFetched);
        _wideEvent.WideEvent?.Set("github.stats.languages_count", languageBytes.Count);

        var totalBytes = languageBytes.Values.Sum();
        var topLanguages = languageBytes
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => new LanguageStat
            {
                Name = kv.Key,
                Bytes = kv.Value,
                Percentage = totalBytes > 0 ? Math.Round((double)kv.Value / totalBytes * 100, 1) : 0,
                Color = GetLanguageColor(kv.Key)
            })
            .ToList();

        var stats = new GitHubStats
        {
            Username = user.Login,
            PublicReposCount = ownedRepos.Count,
            TotalStars = ownedRepos.Sum(r => r.StargazersCount),
            TotalForks = ownedRepos.Sum(r => r.ForksCount),
            Followers = user.Followers,
            Following = user.Following,
            TopLanguages = topLanguages,
            FetchedAt = DateTime.UtcNow
        };

        _memoryCache.Set(cacheKey, stats, CacheDuration);
        return stats;
    }

    public async Task<List<LanguageStat>> GetRepositoryLanguagesAsync(string accessToken, string owner, string repoName, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(accessToken);
        var cacheKey = $"lang_{accessToken.GetHashCode()}_{owner}_{repoName}";

        if (_memoryCache.TryGetValue(cacheKey, out List<LanguageStat>? cached ) && 
            cached is not null)
        {
            _wideEvent.WideEvent?.Set("github.languages.cache_hit", true);
            return cached;
        }

        _wideEvent.WideEvent?.Set("github.languages.cache_hit", false);

        var languages = await client.Repository.GetAllLanguages(owner, repoName);
        var totalBytes = languages.Sum(l => l.NumberOfBytes);

        var result = languages
            .OrderByDescending(l => l.NumberOfBytes)
            .Select(l => new LanguageStat
            {
                Name = l.Name,
                Bytes = l.NumberOfBytes,
                Percentage = totalBytes > 0 ? Math.Round((double)l.NumberOfBytes / totalBytes * 100, 1) : 0,
                Color = GetLanguageColor(l.Name)
            })
            .ToList();

        _memoryCache.Set(cacheKey, result, CacheDuration);
        return result;
    }


    private GitHubClient CreateClient(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("Profily"))
        {
            Credentials = new Credentials(accessToken)
        };

        return client;
    }

    private static GitHubRepository MapToGitHubRepository(Repository repo)
    {
        return new GitHubRepository
        {
            Id = repo.Id,
            Name = repo.Name,
            FullName = repo.FullName,
            Description = repo.Description,
            HtmlUrl = repo.HtmlUrl,
            HomePage = repo.Homepage,
            Language = repo.Language,
            StarsCount = repo.StargazersCount,
            ForksCount = repo.ForksCount,
            IsFork = repo.Fork,
            IsArchived = repo.Archived,
            IsPrivate = repo.Private,
            Topics = repo.Topics?.ToList(),
            CreatedAt = repo.CreatedAt.UtcDateTime,
            UpdatedAt = repo.UpdatedAt.UtcDateTime,
            PushedAt = repo.PushedAt?.UtcDateTime
        };
    }

    private static string? GetLanguageColor(string language)
    {
        // Common GitHub language colors
        return language.ToLower() switch
        {
            "c#" => "#178600",
            "typescript" => "#3178c6",
            "javascript" => "#f1e05a",
            "python" => "#3572A5",
            "java" => "#b07219",
            "go" => "#00ADD8",
            "rust" => "#dea584",
            "html" => "#e34c26",
            "css" => "#563d7c",
            "ruby" => "#701516",
            "php" => "#4F5D95",
            "swift" => "#F05138",
            "kotlin" => "#A97BFF",
            "c++" => "#f34b7d",
            "c" => "#555555",
            _ => null
        };
    }
}