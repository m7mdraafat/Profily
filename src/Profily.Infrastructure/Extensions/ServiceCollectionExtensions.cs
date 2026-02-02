using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profily.Core.Interfaces;
using Profily.Core.Options;
using Profily.Infrastructure.Data;
using Profily.Infrastructure.Services;

namespace Profily.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCosmosDb(configuration);
        services.AddGitHubAuthentication(configuration);
        services.AddProfilyCors(configuration);
        services.AddServices();
        
        return services;
    }
    /// <summary>
    /// Registers Cosmos DB services with the dependency injection container.
    /// </summary>
    private static IServiceCollection AddCosmosDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CosmosDbOptions>(
            configuration.GetSection(CosmosDbOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var cosmosDbOptions = configuration
                .GetSection(CosmosDbOptions.SectionName)
                .Get<CosmosDbOptions>()
                ?? throw new InvalidOperationException($"{CosmosDbOptions.SectionName} configuration section is missing or invalid.");

            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            };

            return new CosmosClient(
                cosmosDbOptions.AccountEndpoint,
                cosmosDbOptions.AccountKey,
                clientOptions);
        });

        services.AddSingleton<ICosmosDbService, CosmosDbService>();

        return services;
    }

    private static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}