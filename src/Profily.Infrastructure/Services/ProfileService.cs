using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using Profily.Core.Models.Profile;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Implementation of user profile configuration management.
/// Handles CRUD operations and deploy history
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly IDocumentRepository _repository;
    private readonly ITemplateService _templateService;
    private readonly ILogger<ProfileService> _logger;

    private const int MaxDeployHistoryLimit = 50;

    public ProfileService(
        IDocumentRepository repository,
        ITemplateService templateService,
        ILogger<ProfileService> logger)
    {
        _repository = repository;
        _templateService = templateService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProfileConfig?> GetUserConfigAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var configId = $"{ProfileConfig.DocumentType}-{userId}";
        return await _repository.GetAsync<ProfileConfig>(
            id: configId,
            partitionKey: userId,
            ct: ct);
    }

    /// <inheritdoc />
    public async Task<ProfileConfig> SaveUserConfigAsync(ProfileConfig config, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config.UserId);

        _logger.LogDebug("Saving profile config for user {UserId}", config.UserId);

        return await _repository.UpsertAsync(config, ct);
    }

    public async Task<ProfileConfig> CreateFromTemplateAsync(string userId, string templateSlug, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateSlug);

        _logger.LogDebug("Creating profile config for user {UserId} from template {TemplateSlug}", userId, templateSlug);

        var template = await _templateService.GetTemplateBySlugAsync(templateSlug, ct)
            ?? throw new ArgumentException($"Template '{templateSlug}' not found or inactive.", nameof(templateSlug));

        var config = ProfileConfig.CreateForUser(userId, template);

        return await _repository.UpsertAsync(config, ct); 
    }

    public async Task DeleteUserConfigAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var configId = $"{ProfileConfig.DocumentType}-{userId}";

        _logger.LogInformation("Deleting profile config for user {UserId}", userId);

        await _repository.DeleteAsync(
            id: configId,
            partitionKey: userId,
            ct: ct);
    }

    public async Task<List<DeployHistory>> GetDeployHistoryAsync(string userId, int limit = 10, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Clamp limit to valid range
        limit = Math.Clamp(limit, 1, MaxDeployHistoryLimit);

        var history = await _repository.QueryAsync<DeployHistory>(
            documentType: DeployHistory.DocumentType,
            partitionKey: userId,
            filter: null,
            ct: ct);

        return history
            .OrderByDescending(d => d.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<DeployHistory?> GetDeployByIdAsync(string deployId, string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deployId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _repository.GetAsync<DeployHistory>(
            id: deployId,
            partitionKey: userId,
            ct: ct);
    }

    public async Task<DeployHistory> RecordDeployAsync(
        string userId,
        ProfileConfig config,
        DeployResult result,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogInformation(
            "Recording deploy for user {UserId}: Success={Success}, Message={Message}",
            userId, result.Success, result.ErrorMessage ?? "Success");

        // Create immutable deploy record
        var deploy = DeployHistory.Create(
            userId: userId,
            config: config,
            result: result);
        
        var savedDeploy = await _repository.UpsertAsync(deploy, ct);
        
        // If successful,, update the config's last deployed timestamp
        if (result.Success)
        {
            config.IsDraft = false;
            config.LastDeployedAt = DateTime.UtcNow;
            // Note TODO: we could also store a config snapshot here for diff comparison
            await _repository.UpsertAsync(config, ct);
        }
        
        return savedDeploy;   
    }
}