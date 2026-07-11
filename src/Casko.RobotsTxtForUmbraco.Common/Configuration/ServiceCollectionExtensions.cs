using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxt(this IServiceCollection services, IConfiguration configuration)
    {
        var robotsTxtConfigurationSection = configuration.GetSection(RobotsTxtOptions.Key);
        var robotsTxtOptions = robotsTxtConfigurationSection.Get<RobotsTxtOptions>();

        if (robotsTxtOptions?.Enabled is not true)
        {
            return services;
        }

        services.AddSingleton<IValidateOptions<RobotsTxtOptions>, RobotsTxtOptionsValidator>();
        services
            .AddOptions<RobotsTxtOptions>()
            .Bind(robotsTxtConfigurationSection)
            .ValidateOnStart();
        services.AddScoped<IRobotsTxtRenderer, RobotsTxtRenderer>();
        services.AddScoped<IRobotsTxtService, DefaultRobotsTxtService>();
        services.AddScoped<IRobotsTxtTextService, DefaultRobotsTxtTextService>();

        return services;
    }
}
