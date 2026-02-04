// Profily.Infrastructure/Data/Seeding/DataSeederExtensions.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Profily.Infrastructure.Data.Seeding;

public static class DataSeederExtensions
{
    /// <summary>
    /// Register all data seeders with DI.
    /// </summary>
    public static IServiceCollection AddDataSeeders(this IServiceCollection services)
    {
        services.AddScoped<IDataSeeder, SectionSeeder>();
        services.AddScoped<IDataSeeder, SectionStyleSeeder>();
        services.AddScoped<IDataSeeder, TemplateSeeder>();
        
        return services;
    }

    /// <summary>
    /// Run all data seeders on application startup.
    /// Call in Program.cs after building the app.
    /// </summary>
    public static async Task SeedDataAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IDataSeeder>>();
        
        logger.LogInformation("Starting data seeding...");

        var seeders = scope.ServiceProvider
            .GetServices<IDataSeeder>()
            .OrderBy(s => s.Priority)
            .ToList();

        foreach (var seeder in seeders)
        {
            try
            {
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running seeder {SeederType}", seeder.GetType().Name);
                throw; // Fail fast - seeding errors should prevent app start
            }
        }

        logger.LogInformation("Data seeding completed successfully");
    }
}