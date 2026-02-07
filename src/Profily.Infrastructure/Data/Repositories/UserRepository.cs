using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Profily.Core.Interfaces;
using User = Profily.Core.Models.User;

namespace Profily.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for User-specific data operations.
/// Uses IDocumentRepository for common CRUD and adds user-specific queries.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IDocumentRepository _documentRepository;
    private readonly Container _container;
    private readonly ILogger<UserRepository> _logger;
    private readonly IWideEventAccessor _wideEvent;

    public UserRepository(
        IDocumentRepository documentRepository,
        CosmosDocumentRepository cosmosRepository, // For direct container access
        ILogger<UserRepository> logger,
        IWideEventAccessor wideEvent)
    {
        _documentRepository = documentRepository;
        _container = cosmosRepository.Container;
        _logger = logger;
        _wideEvent = wideEvent;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        // For users, Id == UserId (partition key)
        return await _documentRepository.GetAsync<User>(userId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByGitHubIdAsync(long gitHubId, CancellationToken ct = default)
    {
        // This is a cross-partition query since we don't know the userId
        // We need direct container access for this specialized query
        try
        {
            var query = _container.GetItemLinqQueryable<User>()
                .Where(u => u.Type == User.DocumentType && u.GitHubId == gitHubId)
                .ToFeedIterator();

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync(ct);

                _wideEvent.WideEvent?.Set("user_repo.github_id_lookup", gitHubId);
                _wideEvent.WideEvent?.Set("user_repo.github_id_lookup_ru", response.RequestCharge);
                _wideEvent.WideEvent?.Set("user_repo.github_id_found", response.Count > 0);
                
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (CosmosException ex)
        {
            _wideEvent.WideEvent?.Set("user_repo.error", ex.Message);
            _logger.LogError(ex, "Error querying user by GitHub ID {GitHubId}", gitHubId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User> UpsertAsync(User user, CancellationToken ct = default)
    {
        return await _documentRepository.UpsertAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        await _documentRepository.DeleteAsync(userId, userId, ct);
        _wideEvent.WideEvent?.Set("user_repo.deleted_user_id", userId);
    }
}
