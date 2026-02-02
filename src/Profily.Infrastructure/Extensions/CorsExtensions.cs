using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profily.Core.Options;

namespace Profily.Infrastructure.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ProfilyCors";

    public static IServiceCollection AddProfilyCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOptions = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (corsOptions?.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins);
                }
                else
                {
                    // Development fallback
                    policy.WithOrigins("http://localhost:5173", "https://localhost:5173");
                }

                policy.AllowCredentials() // Required for cookies
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

        });

        return services;
    }
}