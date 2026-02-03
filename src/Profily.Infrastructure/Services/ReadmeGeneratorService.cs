using System.Text;
using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.GitHub;
using Profily.Core.Models.Profile;
using Profily.Core.Models.Profile.DTOs;
using Scriban;
using Scriban.Runtime;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Generates README.md and associated files from profile configurations.
/// Uses Scriban for template rendering.
/// </summary>
public sealed class ReadmeGeneratorService : IReadmeGeneratorService
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<ReadmeGeneratorService> _logger;

    public ReadmeGeneratorService(
        ITemplateService templateService,
        ILogger<ReadmeGeneratorService> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<GeneratedProfile> GenerateAsync(ProfileConfig config, GitHubStats stats, List<GitHubRepository> repos, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(repos);

        _logger.LogDebug("Generating README for user {UserId}", config.UserId);

        // Get all required styles in one batch query
        var enabledSections = config.Sections
            .Where(s => s.Enabled)
            .OrderBy(s => s.Order)
            .ToList();

        var styleIds = enabledSections.Select(s => s.StyleId).Distinct();
        var styles = await _templateService.GetStylesByIdsAsync(styleIds.ToList(), ct);

        // Build the template context
        var context = BuildTemplateContext(config, stats, repos);

        // Render each section
        var readme = new StringBuilder();
        var workflows = new List<GeneratedWorkflow>();
        var assets = new List<GeneratedAsset>();

        foreach (var sectionConfig in enabledSections)
        {
            if (!styles.TryGetValue(sectionConfig.StyleId, out var style))
            {
                _logger.LogWarning("Style {StyleId} not found, skipping section {SectionId}", sectionConfig.StyleId, sectionConfig.SectionId);
                continue;
            }

            // Render markdown
            var sectionMarkdown = RenderTemplate(style.MarkdownTemplate, context);
            readme.AppendLine(sectionMarkdown);
            readme.AppendLine(); // Add spacing between sections

            // Collect workflows and assets
            foreach (var workflow in style.Workflows)
            {
                var renderedContent = RenderTemplate(workflow.Content, context);
                workflows.Add(new GeneratedWorkflow(workflow.Path, renderedContent));
            }

            foreach (var asset in style.Assets)
            {
                var renderedPath = RenderTemplate(asset.Path, context);
                var renderedContent = RenderTemplate(asset.Content, context);
                assets.Add(new GeneratedAsset(renderedPath, renderedContent, asset.ContentType));
            }
        }

        _logger.LogInformation(
            "Generated profile: {SectionCount} sections, {WorkflowCount} workflows, {AssetCount} assets for user {UserId}",
            enabledSections.Count, workflows.Count, assets.Count, config.UserId);
        
        return new GeneratedProfile
        {
            Readme = readme.ToString(),
            Workflows = workflows,
            Assets = assets
        };
    }

    public async Task<string> GenerateSectionPreviewAsync(SectionStyle style, ThemeConfig theme, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(theme);

        _logger.LogDebug("Generating section preview for style {StyleId}", style.Id);

        // Build preview context with sample data
        var context = BuildPreviewContext(theme);

        // Render markdown
        var previewMarkdown = RenderTemplate(style.MarkdownTemplate, context);

        return previewMarkdown;
    }

    private ScriptObject BuildTemplateContext(
        ProfileConfig config,
        GitHubStats stats,
        List<GitHubRepository> repos)
    {
        var scriptObject = new ScriptObject();

        // User content
        scriptObject.Add("username", config.Content?.DisplayName ?? "Github Username");
        scriptObject.Add("display_name", config.Content?.DisplayName ?? "GitHub display name");
        scriptObject.Add("tagline", config.Content?.Tagline ?? "Your tagline goes here");
        scriptObject.Add("about_me", config.Content?.AboutMe ?? "A short bio about you.");
        scriptObject.Add("skills", config.Content?.CustomSkills ?? new List<string>());

        // Theme
        scriptObject.Add("theme", new ScriptObject
        {
            { "primary", config.Theme.Primary },
            { "secondary", config.Theme.Secondary },
            { "background", config.Theme.Background },
            { "gradient", config.Theme.Gradient ?? "0:EEFF00,100:a82da" },
            { "text_color", config.Theme.TextColor ?? "#ffffff"}
        });

        // GitHub stats
        scriptObject.Add("stats", new ScriptObject
        {
            { "public_repos_count", stats.PublicReposCount },
            { "total_stars", stats.TotalStars },
            { "total_forks", stats.TotalForks },
            { "total_commits", stats.TotalCommits },
            { "total_followers", stats.Followers },
            { "total_following", stats.Following },
        });

        // Repositories (for pinned repos section)
        var repoList = repos.Select(r => new ScriptObject
        {
            { "name", r.Name },
            { "description", r.Description ?? "" },
            { "primary_language", r.Language ?? "Unknown" },
            { "stars", r.StarsCount },
            { "forks", r.ForksCount },
            { "languages", r.Languages ?? new List<string>()},
            { "html_url", r.HtmlUrl }
        }).ToList();
        scriptObject.Add("repositories", repoList);

        // Social links 
        if (config.SocialLinks != null)
        {
            scriptObject.Add("social", new ScriptObject
            {
                { "linkedin", config.SocialLinks.Linkedin ?? "" },
                { "twitter", config.SocialLinks.Twitter ?? "" },
                { "youtube", config.SocialLinks.Youtube ?? "" },
                { "discord", config.SocialLinks.Discord ?? "" },
                { "email", config.SocialLinks.Email ?? "" },
                { "website", config.SocialLinks.Website ?? "" },
                { "leetcode", config.SocialLinks.Leetcode ?? "" },
                { "resume", config.SocialLinks.Resume ?? "" }
            });
        }

        // Preferences
        scriptObject.Add("preferences", new ScriptObject
        {
            { "show_open_to_work", config.Preferences?.ShowOpenToWork ?? false },
            { "show_profile_views", config.Preferences?.ShowProfileViews ?? true }
        });

        return scriptObject;
    }

    private ScriptObject BuildPreviewContext(ThemeConfig theme)
    {
        // Sample data for style previews
        var scriptObject = new ScriptObject
        {
            { "username", "MohamedRaafat"},
            { "display_name", "Mohamed Raafat"},
            { "tagline", "Software Engineer Intern @ Microsoft | Full Stack Developer"}, 
            { "about_me", "Passionate about building impactful software solutions. Experienced in C#, .NET, and cloud technologies."},
            { "skills", new List<string> { "C#", ".NET", "Azure", "JavaScript", "React" } }
        };

        scriptObject.Add("theme", new ScriptObject
        {
            { "primary", theme.Primary },
            { "secondary", theme.Secondary },
            { "background", theme.Background },
            { "gradient", theme.Gradient ?? "0:EEFF00,100:a82da" },
            { "text_color", theme.TextColor ?? "#ffffff"}
        });

        scriptObject.Add("stats", new ScriptObject
        {
            { "total_repos", 23 },
            { "total_stars", 17 },
            { "total_forks", 3 },
            { "total_commits", 1000 },
            { "total_followers", 69 },
            { "total_following", 13 },
        });

        return scriptObject;
    }

    private string RenderTemplate(string template, ScriptObject context)
    {
        try
        {
            var scribanContext = Template.Parse(template);

            if (scribanContext.HasErrors)
            {
                _logger.LogWarning("Template parsing errors: {Errors}", string.Join(", ", scribanContext.Messages));
                return template;
            }

            var templateContext = new TemplateContext();
            
            // Enable all builtin functions (array, string, etc.)
            templateContext.PushGlobal(new ScriptObject());
            templateContext.PushGlobal(context);

            return scribanContext.Render(templateContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template");
            return template;
        }
    }
}