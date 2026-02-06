using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Implementation of template, section, and style queries.
/// No service-level logging needed — CosmosDocumentRepository handles
/// operational metrics (RU charges), and the WideEvent middleware
/// captures request-level context.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    private readonly IDocumentRepository _repository;

    private const string SystemPartition = "system";
    public TemplateService(
        IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProfileTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default)
    {
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

        var templates = await _repository.QueryAsync<ProfileTemplate>(
            documentType: ProfileTemplate.DocumentType,
            partitionKey: SystemPartition,
            t => t.Slug == slug && t.IsActive,
            ct: ct);
        
        return templates.FirstOrDefault();
    }

    public async Task<List<Section>> GetAllSectionsAsync(CancellationToken ct = default)
    {
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
            return new Dictionary<string, SectionStyle>();

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

        var styles = await _repository.QueryAsync<SectionStyle>(
            documentType: SectionStyle.DocumentType,
            partitionKey: SystemPartition,
            t => t.SectionId == sectionId && t.IsActive,
            ct: ct);

        return styles;
    }
}