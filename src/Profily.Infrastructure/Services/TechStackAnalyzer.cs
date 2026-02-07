using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.TechStack;

namespace Profily.Infrastructure.Services;

public sealed partial class TechStackAnalyzer : ITechStackAnalyzer
{
    private readonly IGitHubService _gitHubService;
    private readonly IDocumentRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly IWideEventAccessor _wideEvent;
    private readonly ILogger<TechStackAnalyzer> _logger;
    private readonly FrameworkMappings _mappings;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromHours(24);
    private const string CacheKeyPrefix = "techStack";
    private const int MaxReposToAnalyze = 100;
    private const int MaxSourcesPerTech = 5;
    private const int MaxTechnologies = 200;
    private const int MaxConcurrentRepos = 5; // Bounded parallelism to respect GitHub rate limits

    private static readonly Dictionary<string, string> LanguageIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C#"] = "csharp",
        ["JavaScript"] = "javascript",
        ["TypeScript"] = "typescript",
        ["Python"] = "python",
        ["Java"] = "java",
        ["Go"] = "go",
        ["Rust"] = "rust",
        ["C"] = "c",
        ["C++"] = "cplusplus",
        ["Ruby"] = "ruby",
        ["PHP"] = "php",
        ["Swift"] = "swift",
        ["Kotlin"] = "kotlin",
        ["Dart"] = "dart",
        ["HTML"] = "html5",
        ["CSS"] = "css3",
        ["Shell"] = "bash",
        ["PowerShell"] = "powershell",
        ["Lua"] = "lua",
        ["R"] = "r",
        ["Scala"] = "scala",
        ["Objective-C"] = "objectivec",
        ["MATLAB"] = "matlab",
        ["Perl"] = "perl",
    };

    public TechStackAnalyzer(
        IGitHubService gitHubService,
        IDocumentRepository repository,
        IMemoryCache cache,
        IWideEventAccessor wideEvent,
        ILogger<TechStackAnalyzer> logger,
        FrameworkMappings mappings)
    {
        _gitHubService = gitHubService;
        _repository = repository;
        _cache = cache;
        _wideEvent = wideEvent;
        _logger = logger;
        _mappings = mappings;
    }

    public async Task<TechStackProfile> GetTechStackAsync(string userId, string accessToken, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}_{userId}";

        // 1. Try to get from cache
        if (_cache.TryGetValue(cacheKey, out TechStackProfile? cached) && cached is not null)
        {
            _wideEvent.WideEvent?.Set("techStack.source", "cache");
            return cached;
        }

        // 2. Try to get from Cosmos DB (if not stale)
        var docId = $"{TechStackProfile.DocumentType}-{userId}";
        var stored = await _repository.GetAsync<TechStackProfile>(docId, userId, cancellationToken);

        if (stored is not null && DateTime.UtcNow - stored.AnalyzedAt < StalenessThreshold)
        {
            _wideEvent.WideEvent?.Set("techStack.source", "database");
            _cache.Set(cacheKey, stored, CacheTtl);
            return stored;
        }

        // 3. Perform fresh analysis
        _wideEvent.WideEvent?.Set("techStack.source", "fresh_analysis");
        return await AnalyzeAndPersistAsync(userId, accessToken, cancellationToken);
    }

    public async Task<TechStackProfile> RefreshTechStackAsync(string userId, string accessToken, CancellationToken cancellationToken = default)
    {
        _wideEvent.WideEvent?.Set("techStack.source", "forced_refresh");

        // Evict cache so fresh analysis is stored
        _cache.Remove($"{CacheKeyPrefix}_{userId}");

        return await AnalyzeAndPersistAsync(userId, accessToken, cancellationToken);
    }

    private async Task<TechStackProfile> AnalyzeAndPersistAsync(
        string userId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var repos = await _gitHubService.GetUserRepositoriesAsync(accessToken, cancellationToken);

        var targetRepos = repos
            .Where(r => !r.IsFork)
            .OrderByDescending(r => r.Size)
            .ThenByDescending(r => r.PushedAt)
            .Take(MaxReposToAnalyze)
            .ToList();

        _wideEvent.WideEvent?.Set("techStack.total_repos", repos.Count);
        _wideEvent.WideEvent?.Set("techStack.analyzed_repos", targetRepos.Count);

        // Thread-safe collection for parallel repo processing
        var allDetections = new System.Collections.Concurrent.ConcurrentBag<(string Name, TechCategory Category, string? Icon, string Signal)>();

        // Process repos in parallel with bounded concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrentRepos,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(targetRepos, parallelOptions, async (repo, ct) =>
        {
            var owner = repo.FullName.Split('/')[0];

            // Signal 1 + file tree fetch in parallel (both are independent GitHub API calls)
            var langTask = DetectFromLanguagesAsync(accessToken, owner, repo.Name, ct);
            Task<List<string>> treeTask;

            try
            {
                treeTask = _gitHubService.GetRepoFileTreeAsync(accessToken, owner, repo.Name, ct);
                await Task.WhenAll(langTask, treeTask);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch file tree for {Repo}. Skipping.", repo.FullName);
                // Still collect language detections if they succeeded
                if (langTask.IsCompletedSuccessfully)
                {
                    foreach (var d in langTask.Result)
                        allDetections.Add((d.Name, d.Category, d.Icon, "languages"));
                }
                return;
            }

            // Collect language results
            foreach (var d in langTask.Result)
                allDetections.Add((d.Name, d.Category, d.Icon, "languages"));

            var fileTree = treeTask.Result;

            // Signals 2, 3, 4 in parallel (all depend on fileTree, but are independent of each other)
            var depTask = DetectFromDependenciesAsync(accessToken, owner, repo.Name, fileTree, ct);
            var fileTask = Task.Run(() => DetectFromFilePresence(fileTree), ct);
            var readmeTask = DetectFromReadmeAsync(accessToken, owner, repo.Name, fileTree, ct);

            // Signal 5: repo topics (no API call, already in repo metadata)
            var topicResults = DetectFromTopics(repo.Topics);

            await Task.WhenAll(depTask, fileTask, readmeTask);

            foreach (var d in depTask.Result)
                allDetections.Add((d.Name, d.Category, d.Icon, "dependencies"));
            foreach (var d in fileTask.Result)
                allDetections.Add((d.Name, d.Category, d.Icon, "file_presence"));
            foreach (var d in readmeTask.Result)
                allDetections.Add((d.Name, d.Category, d.Icon, "readme"));
            foreach (var d in topicResults)
                allDetections.Add((d.Name, d.Category, d.Icon, "topics"));
        });

        // Build signal summary from actual signal tags
        var detectionsList = allDetections.ToList();
        var signalSummary = new Dictionary<string, int>
        {
            ["total_detections"] = detectionsList.Count,
            ["languages"] = detectionsList.Count(d => d.Signal == "languages"),
            ["dependencies"] = detectionsList.Count(d => d.Signal == "dependencies"),
            ["file_presence"] = detectionsList.Count(d => d.Signal == "file_presence"),
            ["readme"] = detectionsList.Count(d => d.Signal == "readme"),
            ["topics"] = detectionsList.Count(d => d.Signal == "topics")
        };

        _wideEvent.WideEvent?.Set("techStack.signal_summary", JsonSerializer.Serialize(signalSummary));

        // Log raw detections (before dedup) grouped by signal
        foreach (var signalGroup in detectionsList.GroupBy(d => d.Signal))
        {
            var names = signalGroup.Select(d => $"{d.Name} [{d.Category}]").OrderBy(n => n);
            _logger.LogInformation("[TechStack] Raw {Signal} ({Count}): {Names}",
                signalGroup.Key, signalGroup.Count(), string.Join(", ", names));
        }

        // Aggregate, rank, then group by category
        var flat = detectionsList.Select(d => (d.Name, d.Category, d.Icon)).ToList();
        var technologies = AggregateDetections(flat);

        // Log final result (after dedup + ranking)
        foreach (var catGroup in technologies.GroupBy(t => t.Category))
        {
            var names = catGroup.Select(t => t.Name).OrderBy(n => n);
            _logger.LogInformation("[TechStack] Final {Category} ({Count}): {Names}",
                catGroup.Key, catGroup.Count(), string.Join(", ", names));
        }
        _logger.LogInformation("[TechStack] Dedup: {Raw} raw → {Final} unique technologies",
            detectionsList.Count, technologies.Count);

        var profile = TechStackProfile.CreateForUser(userId);
        profile.Categorized = CategorizedTechStack.FromFlat(technologies);
        profile.AnalyzedRepoCount = targetRepos.Count;
        profile.SignalSummary = signalSummary;

        var saved = await _repository.UpsertAsync(profile, cancellationToken);

        _cache.Set($"{CacheKeyPrefix}_{userId}", saved, CacheTtl);
        return saved;
    }

    private async Task<List<(string Name, TechCategory Category, string? Icon)>> DetectFromLanguagesAsync(
        string accessToken,
        string owner,
        string repoName,
        CancellationToken cancellationToken)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            var languages = await _gitHubService.GetRepositoryLanguagesAsync(accessToken, owner, repoName, cancellationToken);

            foreach (var lang in languages)
            {
                LanguageIcons.TryGetValue(lang.Name, out var icon);
                results.Add((lang.Name, TechCategory.Language, icon));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch languages for {Repo}. Skipping language detection for this repo.", $"{owner}/{repoName}");
        }
        return results;
    }

    private async Task<List<(string Name, TechCategory Category, string? Icon)>> DetectFromDependenciesAsync(
        string accessToken,
        string owner,
        string repoName,
        List<string> fileTree,
        CancellationToken cancellationToken)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        // package.json
        if (fileTree.Any(f => f == "package.json" || f.EndsWith("/package.json")))
        {
            var path = fileTree.First(f => f == "package.json" || f.EndsWith("/package.json"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParsePackageJson(content));
            }
        }

        // *.csproj (check all - multi-project repos)
        var csprojFiles = fileTree.Where(f => f.EndsWith(".csproj")).ToList();
        foreach (var csproj in csprojFiles)
        {
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, csproj, cancellationToken);
            if (content is not null)
            {
                results.AddRange(ParseCsProj(content));
            }
        }

        // requirements.txt
        if (fileTree.Any(f => f == "requirements.txt" || f.EndsWith("/requirements.txt")))
        {
            var path = fileTree.First(f => f == "requirements.txt" || f.EndsWith("/requirements.txt"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParseRequirementsTxt(content));
            }
        }

        // go.mod
        if (fileTree.Any(f => f == "go.mod" || f.EndsWith("/go.mod")))
        {
            var path = fileTree.First(f => f == "go.mod" || f.EndsWith("/go.mod"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParseGoMod(content));
            }
        }

        // TODO: add more dependency file types (pom.xml, Gemfile, etc) in the future

        // pyproject.toml (modern Python - Poetry/PDM/Hatch)
        if (fileTree.Any(f => f == "pyproject.toml" || f.EndsWith("/pyproject.toml")))
        {
            var path = fileTree.First(f => f == "pyproject.toml" || f.EndsWith("/pyproject.toml"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParsePyProjectToml(content));
            }
        }

        // Cargo.toml (Rust)
        if (fileTree.Any(f => f == "Cargo.toml" || f.EndsWith("/Cargo.toml")))
        {
            var path = fileTree.First(f => f == "Cargo.toml" || f.EndsWith("/Cargo.toml"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParseCargoToml(content));
            }
        }

        // pom.xml (Java/Maven)
        if (fileTree.Any(f => f == "pom.xml" || f.EndsWith("/pom.xml")))
        {
            var path = fileTree.First(f => f == "pom.xml" || f.EndsWith("/pom.xml"));
            var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, path, cancellationToken);

            if (content is not null)
            {
                results.AddRange(ParsePomXml(content));
            }
        }

        return results;
    }

    private List<(string Name, TechCategory Category, string? Icon)> ParseGoMod(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            // Only match inside require blocks: require ( ... ) or single require lines
            var requireMatches = GoRequireRegex().Matches(content);
            var requireContent = string.Join("\n", requireMatches.Select(m => m.Groups[1].Value));

            // Also handle single-line: require github.com/gin-gonic/gin v1.9.0
            var singleLines = content.Split('\n')
                .Where(l => l.TrimStart().StartsWith("require ") && !l.Contains('('))
                .ToList();
            requireContent += "\n" + string.Join("\n", singleLines);

            foreach (var (key, mapping) in _mappings.GoMod)
            {
                if (requireContent.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add((mapping.Name, mapping.Category, mapping.Icon));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse go.mod content.");
        }

        return results;
    }

    [GeneratedRegex(@"require\s*\((.*?)\)", RegexOptions.Singleline)]
    private static partial Regex GoRequireRegex();

    private List<(string Name, TechCategory Category, string? Icon)> ParseRequirementsTxt(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip comments, directives, and empty lines
                if (string.IsNullOrWhiteSpace(trimmed)
                    || trimmed.StartsWith('#')
                    || trimmed.StartsWith("-r")
                    || trimmed.StartsWith("--"))
                {
                    continue;
                }

                // Strip extras: package[extra1,extra2]>=1.0 → package
                var nameEnd = trimmed.IndexOfAny(['=', '>', '<', '!', ';', ' ', '[']);
                var packageName = (nameEnd >= 0 ? trimmed[..nameEnd] : trimmed).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(packageName) || packageName.StartsWith('-'))
                {
                    continue;
                }

                if (_mappings.Requirements.TryGetValue(packageName, out var mapping))
                {
                    results.Add((mapping.Name, mapping.Category, mapping.Icon));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse requirements.txt content.");
        }

        return results;
    }

    private List<(string Name, TechCategory Category, string? Icon)> ParsePackageJson(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check dependencies and devDependencies
            foreach (var section in new[] { "dependencies", "devDependencies" })
            {
                if (!root.TryGetProperty(section, out var deps))
                {
                    continue;
                }

                foreach (var dep in deps.EnumerateObject())
                {
                    var packageName = dep.Name.ToLowerInvariant();
                    if (_mappings.PackageJson.TryGetValue(packageName, out var mapping))
                    {
                        results.Add((mapping.Name, mapping.Category, mapping.Icon));
                    }
                }
            }

            // Scan scripts for tool usage hints (e.g., "build": "tsc" → TypeScript)
            if (root.TryGetProperty("scripts", out var scripts))
            {
                var scriptValues = string.Join(" ", scripts.EnumerateObject().Select(s => s.Value.GetString() ?? ""));
                foreach (var (pattern, mapping) in ScriptToolHints)
                {
                    if (scriptValues.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(mapping);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse package.json content.");
        }
        return results;
    }

    /// <summary>
    /// Script command patterns that hint at a tool being used even if not in dependencies.
    /// </summary>
    private static readonly (string Pattern, (string Name, TechCategory Category, string? Icon) Mapping)[] ScriptToolHints =
    [
        ("tsc", ("TypeScript", TechCategory.Language, "typescript")),
        ("nodemon", ("Nodemon", TechCategory.Tool, "nodemon")),
        ("ts-node", ("TypeScript", TechCategory.Language, "typescript")),
        ("next ", ("Next.js", TechCategory.Framework, "nextjs")),
        ("nuxt", ("Nuxt.js", TechCategory.Framework, "nuxtjs")),
        ("tailwind", ("Tailwind CSS", TechCategory.Framework, "tailwindcss")),
        ("prisma ", ("Prisma", TechCategory.Library, "prisma")),
    ];

    private List<(string Name, TechCategory Category, string? Icon)> ParseCsProj(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Extract actual PackageReference includes to avoid false positives from comments/strings
            var matches = PackageReferenceRegex().Matches(content);
            foreach (Match match in matches)
            {
                var packageId = match.Groups[1].Value;
                foreach (var (key, mapping) in _mappings.Csproj)
                {
                    // Prefix match: "Microsoft.EntityFrameworkCore" matches "Microsoft.EntityFrameworkCore.SqlServer"
                    if (packageId.StartsWith(key, StringComparison.OrdinalIgnoreCase) && seen.Add(mapping.Name))
                    {
                        results.Add((mapping.Name, mapping.Category, mapping.Icon));
                    }
                }
            }

            // Also detect target framework (e.g., net8.0 → .NET)
            var tfmMatch = TargetFrameworkRegex().Match(content);
            if (tfmMatch.Success)
            {
                var tfm = tfmMatch.Groups[1].Value;
                if (tfm.StartsWith("net") && !tfm.StartsWith("netstandard") && !tfm.StartsWith("netcoreapp"))
                {
                    if (seen.Add(".NET"))
                    {
                        results.Add((".NET", TechCategory.Framework, "dotnet"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse .csproj content.");
        }

        return results;
    }

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]*)""\s", RegexOptions.IgnoreCase)]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"<TargetFramework>([^<]+)</TargetFramework>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworkRegex();

    // --- New ecosystem parsers ---

    /// <summary>
    /// Parse pyproject.toml for Python dependencies (Poetry, PDM, Hatch, PEP 621).
    /// Matches against the requirements mappings since packages are the same ecosystem.
    /// </summary>
    private List<(string Name, TechCategory Category, string? Icon)> ParsePyProjectToml(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            // Extract dependency names from [project.dependencies], [tool.poetry.dependencies], etc.
            var depMatches = PyProjectDependencyRegex().Matches(content);
            foreach (Match match in depMatches)
            {
                var packageName = match.Groups[1].Value.Trim().ToLowerInvariant();
                if (_mappings.Requirements.TryGetValue(packageName, out var mapping))
                {
                    results.Add((mapping.Name, mapping.Category, mapping.Icon));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse pyproject.toml content.");
        }

        return results;
    }

    [GeneratedRegex(@"(?:^|\n)\s*""?([a-zA-Z0-9_-]+)""?\s*[=>{]", RegexOptions.None)]
    private static partial Regex PyProjectDependencyRegex();

    /// <summary>
    /// Parse Cargo.toml for Rust crate dependencies.
    /// </summary>
    private List<(string Name, TechCategory Category, string? Icon)> ParseCargoToml(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        try
        {
            foreach (var (key, mapping) in _mappings.CargoToml)
            {
                // Match: crate_name = "version" or crate_name = { version = "..." }
                if (Regex.IsMatch(content, $@"(?m)^\s*{Regex.Escape(key)}\s*=", RegexOptions.IgnoreCase))
                {
                    results.Add((mapping.Name, mapping.Category, mapping.Icon));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Cargo.toml content.");
        }

        return results;
    }

    /// <summary>
    /// Parse pom.xml for Java/Maven dependencies via groupId/artifactId matching.
    /// </summary>
    private List<(string Name, TechCategory Category, string? Icon)> ParsePomXml(string content)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var (key, mapping) in _mappings.PomXml)
            {
                // Match groupId or artifactId containing the key
                if (content.Contains(key, StringComparison.OrdinalIgnoreCase) && seen.Add(mapping.Name))
                {
                    results.Add((mapping.Name, mapping.Category, mapping.Icon));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse pom.xml content.");
        }

        return results;
    }

    // Signal 3: file presence (e.g., Dockerfile, .github/workflows/*, etc)
    private List<(string Name, TechCategory Category, string? Icon)> DetectFromFilePresence(List<string> fileTree)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();
        var seen = new HashSet<string>();

        foreach (var file in fileTree)
        {
            var fileName = Path.GetFileName(file);
            if (_mappings.FilePresence.TryGetValue(fileName, out var mapping) && seen.Add(mapping.Name))
            {
                results.Add((mapping.Name, mapping.Category, mapping.Icon));
            }
        }

        // Special case: .github/workflows/* -> GitHub Actions
        if (fileTree.Any(f => f.StartsWith(".github/workflows/", StringComparison.OrdinalIgnoreCase) && f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
        {
            if (seen.Add("GitHub Actions"))
            {
                results.Add(("GitHub Actions", TechCategory.Tool, "githubactions"));
            }
        }

        // Special case: k8s/ or kubernetes/ directory
        if (fileTree.Any(f => f.StartsWith("k8s/", StringComparison.OrdinalIgnoreCase) || f.StartsWith("kubernetes/", StringComparison.OrdinalIgnoreCase)))
        {
            if (seen.Add("Kubernetes"))
            {
                results.Add(("Kubernetes", TechCategory.Tool, "kubernetes"));
            }
        }

        // Extension-based detection: detect technologies by file extensions present in the repo
        foreach (var (extension, mapping) in ExtensionDetections)
        {
            if (fileTree.Any(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) && seen.Add(mapping.Name))
            {
                results.Add(mapping);
            }
        }

        return results;
    }

    /// <summary>
    /// Signal 5: GitHub repo topics.
    /// Topics are user-curated metadata — strong intent signal.
    /// No API call needed; topics are already in the repo metadata from GetUserRepositoriesAsync.
    /// </summary>
    private List<(string Name, TechCategory Category, string? Icon)> DetectFromTopics(List<string>? topics)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();
        if (topics is null or { Count: 0 }) return results;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var topic in topics)
        {
            var normalized = topic.Trim().ToLowerInvariant();
            if (_mappings.TopicMappings.TryGetValue(normalized, out var mapping) && seen.Add(mapping.Name))
            {
                results.Add((mapping.Name, mapping.Category, mapping.Icon));
            }
        }

        return results;
    }

    /// <summary>
    /// File extension patterns that indicate technology usage.
    /// Only checked once per repo (first match wins per tech name via seen set).
    /// </summary>
    private static readonly (string Extension, (string Name, TechCategory Category, string? Icon) Mapping)[] ExtensionDetections =
    [
        (".proto", ("Protobuf", TechCategory.Tool, "protobuf")),
        (".graphql", ("GraphQL", TechCategory.Library, "graphql")),
        (".gql", ("GraphQL", TechCategory.Library, "graphql")),
        (".prisma", ("Prisma", TechCategory.Library, "prisma")),
        (".ipynb", ("Jupyter", TechCategory.Tool, "jupyter")),
        (".bicep", ("Bicep", TechCategory.Tool, "azure")),
        (".razor", ("Blazor", TechCategory.Framework, "blazor")),
        (".vue", ("Vue.js", TechCategory.Framework, "vuejs")),
        (".svelte", ("Svelte", TechCategory.Framework, "svelte")),
        (".tsx", ("React", TechCategory.Framework, "react")),
        (".jsx", ("React", TechCategory.Framework, "react")),
    ];

    // Signal 4: README content analysis (look for tech mentions in README)
    private async Task<List<(string Name, TechCategory Category, string? Icon)>> DetectFromReadmeAsync(
        string accessToken,
        string owner,
        string repoName,
        List<string> fileTree,
        CancellationToken cancellationToken)
    {
        var results = new List<(string Name, TechCategory Category, string? Icon)>();

        var readmePath = fileTree.FirstOrDefault(f => f.Equals("README.md", StringComparison.OrdinalIgnoreCase));

        if (readmePath is null)
        {
            return results;
        }

        var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repoName, readmePath, cancellationToken);
        if (content is null || content.Length > 50_000) // skip very large READMEs
        {
            return results;
        }

        // Detect shields.io badges: img.shields.io/badge/{tech}- or similar patterns
        var badgeMatches = BadgeRegex().Matches(content);
        foreach (Match match in badgeMatches)
        {
            var techName = match.Groups[1].Value.Replace("%20", " ").Replace("_", " ").Trim();
            // Only add if it matches a known technology in our mappings (to avoid false positives from generic badges)
            if (IsKnownTechnology(techName))
            {
                var canonical = NormalizeTechName(techName);

                // Look up the correct category and icon from mappings instead of guessing
                if (_mappings.TechInfoByName.TryGetValue(canonical, out var info))
                {
                    results.Add((canonical, info.Category, info.Icon));
                }
                else
                {
                    results.Add((canonical, TechCategory.Other, null));
                }
            }
        }

        // Scan README sections (## Tech Stack, ## Built With, etc.) for known technology names
        ScanReadmeTechSections(content, results);

        return results;
    }

    private bool IsKnownTechnology(string name)
    {
        var normalized = name.ToLowerInvariant();
        return _mappings.AllKnownNames.Contains(normalized);
    }

    private string NormalizeTechName(string name)
    {
        var normalized = name.ToLowerInvariant();
        return _mappings.NameNormalization.TryGetValue(
            normalized, out var canonical
        ) ? canonical : name;
    }

    [GeneratedRegex(@"img\.shields\.io/badge/([^-/]+)-", RegexOptions.IgnoreCase)]
    private static partial Regex BadgeRegex();

    [GeneratedRegex(@"#+\s*(?:tech(?:nolog(?:y|ies))?\s*stack|built\s*with|technologies(?:\s*used)?|tools?\s*(?:used|&|and)|stack|powered\s*by)\s*\n([\s\S]*?)(?=\n#+\s|\z)", RegexOptions.IgnoreCase)]
    private static partial Regex TechSectionRegex();

    /// <summary>
    /// Scans README tech stack sections for known technology names using word-boundary matching.
    /// Only searches within identified sections to avoid false positives.
    /// </summary>
    private void ScanReadmeTechSections(string readmeContent, List<(string Name, TechCategory Category, string? Icon)> results)
    {
        var sectionMatches = TechSectionRegex().Matches(readmeContent);
        if (sectionMatches.Count == 0) return;

        var sectionText = string.Join("\n", sectionMatches.Select(m => m.Groups[1].Value));
        var alreadySeen = new HashSet<string>(results.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var knownName in _mappings.AllKnownNames)
        {
            if (knownName.Length < 2) continue;

            var canonical = NormalizeTechName(knownName);
            if (alreadySeen.Contains(canonical)) continue;

            var escaped = Regex.Escape(knownName);
            if (Regex.IsMatch(sectionText, $@"(?<![a-zA-Z0-9]){escaped}(?![a-zA-Z0-9])", RegexOptions.IgnoreCase))
            {
                if (_mappings.TechInfoByName.TryGetValue(canonical, out var info))
                {
                    results.Add((canonical, info.Category, info.Icon));
                }
                else
                {
                    results.Add((canonical, TechCategory.Other, null));
                }
                alreadySeen.Add(canonical);
            }
        }
    }

    // Aggregation
    private static List<DetectedTechnology> AggregateDetections(List<(string Name, TechCategory Category, string? Icon)> detections)
    {
        // Group by normalized name
        var grouped = detections
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
        
        var technologies = new List<DetectedTechnology>();

        foreach (var group in grouped)
        {
            var items = group.ToList();

            // Pick the most specific category (prefer non-Other, then highest-frequency)
            var category = items
                .GroupBy(i => i.Category)
                .OrderByDescending(g => g.Key != TechCategory.Other) // non-Other first
                .ThenByDescending(g => g.Count()) // then most frequent
                .First().Key;

            var icon = items.Select(i => i.Icon).FirstOrDefault(i => i is not null);

            technologies.Add(new DetectedTechnology
            {
                Name = group.Key,
                Category = category,
                Icon = icon
            });
        }

        return technologies
            .OrderByDescending(t => detections.Count(d => string.Equals(d.Name, t.Name, StringComparison.OrdinalIgnoreCase))) // frequency
            .ThenBy(t => t.Name) // then alphabetically
            .Take(MaxTechnologies)
            .ToList();
    }
}