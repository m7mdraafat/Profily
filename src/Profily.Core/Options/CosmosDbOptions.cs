namespace Profily.Core.Options;

/// <summary>
/// Configuration options for Azure Cosmos DB connection.
/// </summary>
public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";
    
    /// <summary>
    /// The Cosmos DB account endpoint URI.
    /// </summary>
    public required string AccountEndpoint { get; init; }
    
    /// <summary>
    /// The Cosmos DB account key. Consider using Azure Identity (DefaultAzureCredential) in production.
    /// </summary>
    public required string AccountKey { get; init; }
    
    /// <summary>
    /// The name of the Cosmos DB database.
    /// </summary>
    public required string DatabaseName { get; init; }
    
    /// <summary>
    /// The name of the container within the database.
    /// </summary>
    public required string ContainerName { get; init; }
}