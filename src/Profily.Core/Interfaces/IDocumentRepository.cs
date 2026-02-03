using System.Linq.Expressions;
using Profily.Core.Models;

namespace Profily.Core.Interfaces;

/// <summary>
/// Generic repository for Cosmos DB document operations.
/// Provides type-safe CRUD operations for any CosmosDocument.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Get a document by ID and partition key.
    /// Use when you know the exact document location.
    /// </summary>
    /// <typeparam name="T">Document type (must inherit from CosmosDocument)</typeparam>
    /// <param name="id">Document ID</param>
    /// <param name="partitionKey">Partition key value (userId or "system")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Document if found, null otherwise</returns>
    Task<T?> GetAsync<T>(string id, string partitionKey, CancellationToken ct = default) 
        where T : CosmosDocument;

    /// <summary>
    /// Query documents by type with optional filtering.
    /// Automatically filters by document type discriminator.
    /// </summary>
    /// <typeparam name="T">Document type to query</typeparam>
    /// <param name="documentType">Type discriminator value (e.g., "section", "template")</param>
    /// <param name="partitionKey">Optional partition key to scope query (improves performance)</param>
    /// <param name="filter">Optional LINQ expression for additional filtering</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching documents</returns>
    Task<List<T>> QueryAsync<T>(
        string documentType,
        string? partitionKey = null,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken ct = default) where T : CosmosDocument;

    /// <summary>
    /// Insert or update a document.
    /// Automatically updates the UpdatedAt timestamp.
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    /// <param name="document">Document to upsert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The upserted document with server-side changes</returns>
    Task<T> UpsertAsync<T>(T document, CancellationToken ct = default) 
        where T : CosmosDocument;

    /// <summary>
    /// Delete a document by ID and partition key.
    /// Silently succeeds if document doesn't exist.
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="partitionKey">Partition key value</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteAsync(string id, string partitionKey, CancellationToken ct = default);

    /// <summary>
    /// Batch upsert multiple documents efficiently.
    /// Uses Cosmos bulk operations for better throughput and lower RU cost.
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    /// <param name="documents">Documents to upsert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of upserted documents</returns>
    Task<List<T>> BatchUpsertAsync<T>(IEnumerable<T> documents, CancellationToken ct = default) 
        where T : CosmosDocument;
}
