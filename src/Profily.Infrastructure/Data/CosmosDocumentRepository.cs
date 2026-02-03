using System.Linq.Expressions;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Profily.Core.Interfaces;
using Profily.Core.Models;
using Profily.Core.Options;

namespace Profily.Infrastructure.Data;

/// <summary>
/// Generic Cosmos DB document repository.
/// Provides low-level CRUD operations for any CosmosDocument type.
/// 
/// Domain-specific repositories (UserRepository, ProfileConfigRepository, etc.)
/// should use this class for common operations and add their own specialized queries.
/// </summary>
public sealed class CosmosDocumentRepository : IDocumentRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDocumentRepository> _logger;

    /// <summary>
    /// Exposes internal container for specialized repositories that need direct access.
    /// Use sparingly - prefer using the generic methods.
    /// </summary>
    internal Container Container => _container;

    public CosmosDocumentRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbOptions> options,
        ILogger<CosmosDocumentRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options.Value);
        _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string id, string partitionKey, CancellationToken ct = default)
        where T : CosmosDocument
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(
                id: id,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: ct
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Document {Id} not found in partition {PartitionKey}", id, partitionKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> QueryAsync<T>(
        string documentType,
        string? partitionKey = null,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken ct = default) where T : CosmosDocument
    {
        var queryable = _container.GetItemLinqQueryable<T>(
            requestOptions: partitionKey is not null
                ? new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) }
                : null
        );

        // Filter by document type
        var query = queryable.Where(d => d.Type == documentType);

        // Apply additional filter if provided
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var iterator = query.ToFeedIterator();
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            results.AddRange(response);
            
            _logger.LogDebug("Query for {Type} returned {Count} items, RU: {RU}",
                documentType, response.Count, response.RequestCharge);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<T> UpsertAsync<T>(T document, CancellationToken ct = default)
        where T : CosmosDocument
    {
        document.UpdatedAt = DateTime.UtcNow;

        var response = await _container.UpsertItemAsync(
            item: document,
            partitionKey: new PartitionKey(document.UserId),
            cancellationToken: ct
        );

        _logger.LogDebug("Upserted {Type} {Id}, RU: {RU}",
            document.Type, document.Id, response.RequestCharge);

        return response.Resource;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, string partitionKey, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.DeleteItemAsync<CosmosDocument>(
                id: id,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: ct
            );

            _logger.LogDebug("Deleted {Id} from partition {PartitionKey}, RU: {RU}",
                id, partitionKey, response.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Document {Id} not found for deletion", id);
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> BatchUpsertAsync<T>(IEnumerable<T> documents, CancellationToken ct = default)
        where T : CosmosDocument
    {
        var results = new List<T>();
        var documentList = documents.ToList();

        if (documentList.Count == 0)
            return results;

        // Group by partition key for efficient batch operations
        var grouped = documentList.GroupBy(d => d.UserId);
        var totalRu = 0.0;

        foreach (var group in grouped)
        {
            foreach (var document in group)
            {
                ct.ThrowIfCancellationRequested();
                document.UpdatedAt = DateTime.UtcNow;

                var response = await _container.UpsertItemAsync(
                    item: document,
                    partitionKey: new PartitionKey(document.UserId),
                    cancellationToken: ct
                );

                results.Add(response.Resource);
                totalRu += response.RequestCharge;
            }
        }

        _logger.LogInformation("Batch upserted {Count} documents, total RU: {RU}",
            documentList.Count, totalRu);

        return results;
    }
}