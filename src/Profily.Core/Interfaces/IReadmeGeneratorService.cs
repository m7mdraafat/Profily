using Profily.Core.Models.GitHub;
using Profily.Core.Models.Profile;
using Profily.Core.Models.Profile.DTOs;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for generating README.md and associated files from a profile configuration.
/// Combines user config, GitHub data, and style templates into deployable output.
/// </summary>
public interface IReadmeGeneratorService
{
    /// <summary>
    /// Generate complete profile output from configuration and GitHub data.
    /// 
    /// Process:
    /// 1. Load all required section styles
    /// 2. Build template context from config, stats, and repos
    /// 3. Render each enabled section's markdown template
    /// 4. Collect required GitHub workflows and assets
    /// 5. Return combined output ready for deployment
    /// </summary>
    /// <param name="config">User's profile configuration</param>
    /// <param name="stats">User's GitHub statistics</param>
    /// <param name="repos">User's repositories (for pinned repos section)</param>
    /// <returns>Generated README, workflows, and assets</returns>
    Task<GeneratedProfile> GenerateAsync(
        ProfileConfig config,
        GitHubStats stats,
        List<GitHubRepository> repos,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a preview of a single section with sample data.
    /// Used in the style picker to show how a style looks.
    /// </summary>
    /// <param name="style">Style to preview</param>
    /// <param name="theme">Theme to apply</param>
    /// <returns>Rendered markdown for the section</returns>
    Task<string> GenerateSectionPreviewAsync(
        SectionStyle style,
        ThemeConfig theme,
        CancellationToken ct = default);
}