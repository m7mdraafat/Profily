using Profily.Core.Models.Profile;

namespace Profily.Core.Interfaces;

/// <summary>
/// Service for reading, template, section, and style data.
/// All operations are read-only - templates are managed by admin seeding.
/// </summary>

public interface ITemplateService
{
    /// <summary>
    /// Get all active templates for display in the template gallery.
    /// Results are ordered by IsOfficial (true first), then by UsageCount descending.
    /// </summary>
    Task<List<ProfileTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific template by its URL-safe slug.
    /// </summary>
    /// <param name="slug">URL-safe template identifier (e.g., "developer-profile")</param>
    /// <returns> ProfileTemplate if found, null otherwise</returns>
    Task<ProfileTemplate?> GetTemplateBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Get all active sections for the section picker.
    /// </summary>
    /// <returns>List of active sections</returns>
    Task<List<Section>> GetAllSectionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific section by its URL-safe slug.
    /// </summary>
    /// <param name="slug">URL-safe section identifier (e.g., "contact-info")</param>
    /// <returns>Section if found, null otherwise</returns>
    Task<Section?> GetSectionBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Get all active styles for a specific section.
    /// Used when user wants to change the visual variant of a section.
    /// </summary>
    /// <param name="sectionId">Section Id (e.g., "section-header")</param>
    /// <returns>List of styles for the section</returns>
    Task<List<SectionStyle>> GetStylesForSectionAsync(string sectionId, CancellationToken ct = default);

    /// <summary>
    /// Get a specific style by its document ID.
    /// Used during README generation to load the markdown template.
    /// </summary>
    /// <param name="styleId">Style Id (e.g., "sectionStyle-header-typing.svg")</param>
    /// <returns>Section style if found, null otherwise</returns>
    Task<SectionStyle?> GetStyleByIdAsync(string styleId, CancellationToken ct = default);

    /// <summary>
    /// Batch load multiple styles by their IDs.
    /// More efficient than multiple single fetches calls during README generation.
    /// </summary>
    Task<Dictionary<string, SectionStyle>> GetStylesByIdsAsync(IEnumerable<string> styleIds, CancellationToken ct = default);
}