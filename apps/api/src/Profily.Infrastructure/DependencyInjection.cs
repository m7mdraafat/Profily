using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;
using Profily.Infrastructure.Services;
using Profily.Infrastructure.Settings;

namespace Profily.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProfilyDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .UseSnakeCaseNamingConvention();
        });

        // Settings
        services.Configure<GitHubSettings>(configuration.GetSection(GitHubSettings.SectionName));
        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));

        // Token encryption
        services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();

        // Session service
        services.AddScoped<ISessionService, SessionService>();

        // GitHub auth
        services.AddSingleton<IGitHubAuthService, GitHubAuthService>();

        return services;
    }
}