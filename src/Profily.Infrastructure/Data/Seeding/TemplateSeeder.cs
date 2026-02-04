// Profily.Infrastructure/Data/Seeding/TemplateSeeder.cs

using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Data.Seeding;

public sealed class TemplateSeeder : IDataSeeder
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<TemplateSeeder> _logger;

    public int Priority => 3; // Runs after sections and styles

    public TemplateSeeder(IDocumentRepository repository, ILogger<TemplateSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding templates...");

        var templates = GetTemplates();
        var existingCount = 0;
        var createdCount = 0;

        foreach (var template in templates)
        {
            var existing = await _repository.GetAsync<ProfileTemplate>(template.Id, template.UserId, ct);
            if (existing is not null)
            {
                existingCount++;
                continue;
            }

            await _repository.UpsertAsync(template, ct);
            createdCount++;
        }

        _logger.LogInformation(
            "Template seeding complete: {Created} created, {Existing} already existed",
            createdCount, existingCount);
    }

    private static List<ProfileTemplate> GetTemplates() =>
    [
        // Developer Pro - Full featured
        new ProfileTemplate
        {
            Id = "template-developer-pro",
            UserId = "system",
            Slug = "developer-pro",
            DisplayName = "Developer Pro",
            Description = "Full-featured profile with stats, 3D contributions, snake animation, and more",
            Icon = "🚀",
            IsOfficial = true,
            IsActive = true,
            CreatedBy = "admin",
            Theme = DarkTheme,
            Sections =
            [
                new TemplateSectionConfig { SectionId = "section-header", StyleId = "sectionStyle-header-capsule-render", Order = 0 },
                new TemplateSectionConfig { SectionId = "section-badges", StyleId = "sectionStyle-badges-shields", Order = 1 },
                new TemplateSectionConfig { SectionId = "section-about", StyleId = "sectionStyle-about-bullets", Order = 2 },
                new TemplateSectionConfig { SectionId = "section-skills", StyleId = "sectionStyle-skills-icons", Order = 3 },
                new TemplateSectionConfig { SectionId = "section-stats", StyleId = "sectionStyle-stats-combined", Order = 4 },
                new TemplateSectionConfig { SectionId = "section-streak", StyleId = "sectionStyle-streak-card", Order = 5 },
                new TemplateSectionConfig { SectionId = "section-snake", StyleId = "sectionStyle-snake-dark", Order = 6 },
                new TemplateSectionConfig { SectionId = "section-repos", StyleId = "sectionStyle-repos-cards", Order = 7 },
                new TemplateSectionConfig { SectionId = "section-contact", StyleId = "sectionStyle-contact-badges", Order = 8 },
                new TemplateSectionConfig { SectionId = "section-footer", StyleId = "sectionStyle-footer-wave", Order = 9 }
            ]
        },
        
        // Minimal Clean - Simple and clean
        new ProfileTemplate
        {
            Id = "template-minimal-clean",
            UserId = "system",
            Slug = "minimal-clean",
            DisplayName = "Minimal Clean",
            Description = "Clean and simple profile with essential sections only",
            Icon = "✨",
            IsOfficial = true,
            IsActive = true,
            CreatedBy = "admin",
            Theme = LightTheme,
            Sections =
            [
                new TemplateSectionConfig { SectionId = "section-header", StyleId = "sectionStyle-header-simple", Order = 0 },
                new TemplateSectionConfig { SectionId = "section-about", StyleId = "sectionStyle-about-quote", Order = 1 },
                new TemplateSectionConfig { SectionId = "section-skills", StyleId = "sectionStyle-skills-simple", Order = 2 },
                new TemplateSectionConfig { SectionId = "section-stats", StyleId = "sectionStyle-stats-card", Order = 3 },
                new TemplateSectionConfig { SectionId = "section-contact", StyleId = "sectionStyle-contact-icons", Order = 4 }
            ]
        },
        
        // Creative Fun - Animated and colorful
        new ProfileTemplate
        {
            Id = "template-creative-fun",
            UserId = "system",
            Slug = "creative-fun",
            DisplayName = "Creative Fun",
            Description = "Colorful profile with animations and visual flair",
            Icon = "🎨",
            IsOfficial = true,
            IsActive = true,
            CreatedBy = "admin",
            Theme = ColorfulTheme,
            Sections =
            [
                new TemplateSectionConfig { SectionId = "section-header", StyleId = "sectionStyle-header-typing-svg", Order = 0 },
                new TemplateSectionConfig { SectionId = "section-badges", StyleId = "sectionStyle-badges-social", Order = 1 },
                new TemplateSectionConfig { SectionId = "section-stats", StyleId = "sectionStyle-stats-trophy", Order = 2 },
                new TemplateSectionConfig { SectionId = "section-snake", StyleId = "sectionStyle-snake-green", Order = 3 },
                new TemplateSectionConfig { SectionId = "section-skills", StyleId = "sectionStyle-skills-icons", Order = 4 },
                new TemplateSectionConfig { SectionId = "section-repos", StyleId = "sectionStyle-repos-cards", Order = 5 },
                new TemplateSectionConfig { SectionId = "section-footer", StyleId = "sectionStyle-footer-wave", Order = 6 }
            ]
        }
    ];

    #region Theme Presets

    private static readonly ThemeConfig DarkTheme = new()
    {
        Id = "dark",
        Name = "Dark",
        Primary = "#58a6ff",
        Secondary = "#8b949e",
        Background = "#0d1117",
        Gradient = "0:0d1117,100:161b22",
        TextColor = "#ffffff"
    };

    private static readonly ThemeConfig LightTheme = new()
    {
        Id = "light",
        Name = "Light",
        Primary = "#0969da",
        Secondary = "#57606a",
        Background = "#ffffff",
        Gradient = "0:ffffff,100:f6f8fa",
        TextColor = "#24292f"
    };

    private static readonly ThemeConfig ColorfulTheme = new()
    {
        Id = "colorful",
        Name = "Colorful",
        Primary = "#f97316",
        Secondary = "#06b6d4",
        Background = "#18181b",
        Gradient = "0:f97316,50:ec4899,100:8b5cf6",
        TextColor = "#ffffff"
    };

    #endregion
}