using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Profily.Core.Interfaces;
using Profily.Core.Options;
using Profily.Core.Models;
using User = Profily.Core.Models.User;
using Microsoft.Azure.Cosmos.Linq;

namespace Profily.Infrastructure.Data;

public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(
    CosmosClient cosmosClient, 
    IOptions<CosmosDbOptions> options, 
    ILogger<CosmosDbService> logger)
{
    ArgumentNullException.ThrowIfNull(options.Value);
    _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
    _logger = logger;
}

    public async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<User>(
                id: userId,
                partitionKey: new PartitionKey(userId), // Partition key is /userId which mirrors id
                cancellationToken: cancellationToken
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("User {UserId} not found", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByGitHubIdAsync(long githubId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _container.GetItemLinqQueryable<User>()
                .Where(u => u.Type == "user" && u.GitHubId == githubId)
                .ToFeedIterator();

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync(cancellationToken);
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error querying user by GitHub ID {GitHubId}", githubId);
            throw;
        }
    }

    public async Task<User> UpsertUserAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        var response = await _container.UpsertItemAsync(
            item: user,
            partitionKey: new PartitionKey(user.UserId),
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Upserted user {UserId} ({GitHubUsername}), RU cost: {RUCost}", 
            user.Id, user.GitHubUsername, response.RequestCharge);

        return response.Resource;
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<User>(
                id: userId,
                partitionKey: new PartitionKey(userId), // Partition key is /userId which mirrors id
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("Deleted user {UserId}", userId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Attempted to delete non-existent user {UserId}", userId);
        }
    }

}