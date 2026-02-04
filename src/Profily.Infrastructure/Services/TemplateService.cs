using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Implementation of template, section, and style queries.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<TemplateService> _logger;

    private const string SystemPartition = "system";
    public TemplateService(
        IDocumentRepository repository,
        ILogger<TemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ProfileTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching active templates");

        var templates = await _repository.QueryAsync<ProfileTemplate>(
            documentType: ProfileTemplate.DocumentType,
            partitionKey: SystemPartition,
            ct: ct);
        
        // Order: official first, then by popularity
        return templates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.IsOfficial)
            .ThenByDescending(t => t.UsageCount)
            .ToList();
    }

    public async Task<ProfileTemplate?> GetTemplateBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        _logger.LogDebug("Fetching template by slug: {Slug}", slug);

        var templates = await _repository.QueryAsync<ProfileTemplate>(
            documentType: ProfileTemplate.DocumentType,
            partitionKey: SystemPartition,
            t => t.Slug == slug && t.IsActive,
            ct: ct);
        
        return templates.FirstOrDefault();
    }

    public async Task<List<Section>> GetAllSectionsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all active sections");

        var sections = await _repository.QueryAsync<Section>(
            documentType: Section.DocumentType,
            partitionKey: SystemPartition,
            t => t.IsActive,
            ct: ct);

        return sections.OrderBy(s => s.SortOrder).ToList();
    }

    public async Task<Section?> GetSectionBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        _logger.LogDebug("Fetching section by slug: {Slug}", slug);

        var sections = await _repository.QueryAsync<Section>(
            documentType: Section.DocumentType,
            partitionKey: SystemPartition,
            t => t.Slug == slug && t.IsActive,
            ct: ct);
        
        return sections.FirstOrDefault();
    }

    public async Task<SectionStyle?> GetStyleByIdAsync(string styleId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleId);
        _logger.LogDebug("Fetching style by ID: {StyleId}", styleId);

        var styles = await _repository.QueryAsync<SectionStyle>(
            documentType: SectionStyle.DocumentType,
            partitionKey: SystemPartition,
            t => t.Id == styleId && t.IsActive,
            ct: ct);
        
        return styles.FirstOrDefault();
    }

    public async Task<Dictionary<string, SectionStyle>> GetStylesByIdsAsync(IEnumerable<string> styleIds, CancellationToken ct = default)
    {
        var ids = styleIds.ToList();
        if (ids.Count == 0)
        {
            _logger.LogDebug("No style IDs provided for batch fetch");
            return new Dictionary<string, SectionStyle>();
        }

        _logger.LogDebug("Batch fetching styles by IDs: {StyleIds}", string.Join(", ", ids));

        // Query all styles and filter in memory (more efficient than N queries)
        var allStyles = await _repository.QueryAsync<SectionStyle>(
            documentType: SectionStyle.DocumentType,
            partitionKey: SystemPartition,
            t => t.IsActive,
            ct: ct);
        
        return allStyles
            .Where(s => ids.Contains(s.Id))
            .ToDictionary(s => s.Id);
    }

    public async Task<List<SectionStyle>> GetStylesForSectionAsync(string sectionId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);

        _logger.LogDebug("Fetching styles for section ID: {SectionId}", sectionId);

        var styles = await _repository.QueryAsync<SectionStyle>(
            documentType: SectionStyle.DocumentType,
            partitionKey: SystemPartition,
            t => t.SectionId == sectionId && t.IsActive,
            ct: ct);

        return styles;
    }
}