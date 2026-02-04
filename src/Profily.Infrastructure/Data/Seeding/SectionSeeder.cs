// Profily.Infrastructure/Data/Seeding/SectionSeeder.cs

using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Data.Seeding;

public sealed class SectionSeeder : IDataSeeder
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<SectionSeeder> _logger;

    public int Priority => 1; // Runs first

    public SectionSeeder(IDocumentRepository repository, ILogger<SectionSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding sections...");

        var sections = GetSections();
        var existingCount = 0;
        var createdCount = 0;

        foreach (var section in sections)
        {
            var existing = await _repository.GetAsync<Section>(section.Id, section.UserId, ct);
            if (existing is not null)
            {
                existingCount++;
                continue;
            }

            await _repository.UpsertAsync(section, ct);
            createdCount++;
        }

        _logger.LogInformation(
            "Section seeding complete: {Created} created, {Existing} already existed",
            createdCount, existingCount);
    }

    private static List<Section> GetSections() =>
    [
        new Section
        {
            Id = "section-header",
            UserId = "system",
            Slug = "header",
            DisplayName = "Header / Title",
            Description = "Eye-catching header with your name and a wave animation",
            Icon = "👋",
            SortOrder = 0,
            RequiredDataFields = ["display_name"]
        },
        new Section
        {
            Id = "section-badges",
            UserId = "system",
            Slug = "badges",
            DisplayName = "Profile Badges",
            Description = "Show profile views, followers, and social badges",
            Icon = "🏆",
            SortOrder = 1,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-about",
            UserId = "system",
            Slug = "about",
            DisplayName = "About Me",
            Description = "Tell visitors about yourself",
            Icon = "👤",
            SortOrder = 2,
            RequiredDataFields = ["about_me"]
        },
        new Section
        {
            Id = "section-skills",
            UserId = "system",
            Slug = "skills",
            DisplayName = "Tech Stack",
            Description = "Showcase your programming languages and tools",
            Icon = "💻",
            SortOrder = 3,
            RequiredDataFields = ["skills"]
        },
        new Section
        {
            Id = "section-stats",
            UserId = "system",
            Slug = "stats",
            DisplayName = "GitHub Stats",
            Description = "Display your GitHub statistics cards",
            Icon = "📊",
            SortOrder = 4,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-streak",
            UserId = "system",
            Slug = "streak",
            DisplayName = "Contribution Streak",
            Description = "Show your GitHub contribution streak",
            Icon = "🔥",
            SortOrder = 5,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-languages",
            UserId = "system",
            Slug = "languages",
            DisplayName = "Top Languages",
            Description = "Display your most used programming languages",
            Icon = "📝",
            SortOrder = 6,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-snake",
            UserId = "system",
            Slug = "snake",
            DisplayName = "Contribution Snake",
            Description = "Animated snake eating your contributions",
            Icon = "🐍",
            SortOrder = 7,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-3d",
            UserId = "system",
            Slug = "3d-contrib",
            DisplayName = "3D Contributions",
            Description = "3D visualization of your contribution graph",
            Icon = "🎮",
            SortOrder = 8,
            RequiredDataFields = ["username"]
        },
        new Section
        {
            Id = "section-repos",
            UserId = "system",
            Slug = "repos",
            DisplayName = "Top Repositories",
            Description = "Showcase your pinned or top repositories",
            Icon = "📁",
            SortOrder = 9,
            RequiredDataFields = ["repositories"]
        },
        new Section
        {
            Id = "section-contact",
            UserId = "system",
            Slug = "contact",
            DisplayName = "Contact Links",
            Description = "Social media and contact information",
            Icon = "📫",
            SortOrder = 10,
            RequiredDataFields = ["social"]
        },
        new Section
        {
            Id = "section-footer",
            UserId = "system",
            Slug = "footer",
            DisplayName = "Footer",
            Description = "Profile footer with wave or simple line",
            Icon = "👋",
            SortOrder = 11,
            RequiredDataFields = []
        }
    ];
}