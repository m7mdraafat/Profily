namespace Profily.Core.Models.Profile.DTOs;

/// <summary>
/// Output of the README generation process.
/// Contains all files needed to deploy a profile to GitHub.
/// </summary>
public sealed class GeneratedProfile
{
    /// <summary>
    /// The complete README.md content, ready to write to file.
    /// </summary>
    public required string Readme { get; init; }

    /// <summary>
    /// GitHub Actions workflow files required by the profile.
    /// Example: snake animation needs a scheduled workflow.
    /// </summary>
    public List<GeneratedWorkflow> Workflows { get; init; } = [];

    /// <summary>
    /// Static asset files needed by the profile.
    /// Example: custom SVG images, pre-generated graphics.
    /// </summary>
    public List<GeneratedAsset> Assets { get; init; } = [];

    /// <summary>
    /// Total number of files to deploy.
    /// </summary>
    public int TotalFileCount => 1 + Workflows.Count + Assets.Count;
}

/// <summary>
/// A GitHub Actions workflow file to deploy.
/// </summary>
/// <param name="Path">Repository-relative path (e.g., ".github/workflows/snake.yml")</param>
/// <param name="Content">Complete YAML content</param>
public sealed record GeneratedWorkflow(string Path, string Content);

/// <summary>
/// A static asset file to deploy.
/// </summary>
/// <param name="Path">Repository-relative path (e.g., "assets/banner.svg")</param>
/// <param name="Content">File content (base64 for binary, raw for text)</param>
/// <param name="ContentType">MIME type hint: "svg", "png", "gif", "json"</param>
public sealed record GeneratedAsset(string Path, string Content, string ContentType);