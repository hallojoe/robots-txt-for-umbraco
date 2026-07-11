using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.AddSingleton<IRobotsTxtBindingFileResolver, ContentRootRobotsTxtBindingFileResolver>();
        services.AddSingleton<IValidateOptions<RobotsTxtOptions>>(serviceProvider =>
            new RobotsTxtOptionsValidator(serviceProvider.GetRequiredService<IRobotsTxtBindingFileResolver>()));
        services
            .AddOptions<RobotsTxtOptions>()
            .Bind(robotsTxtConfigurationSection)
            .ValidateOnStart();
        services.TryAddScoped<IRobotsTxtRenderer, RobotsTxtRenderer>();
        services.TryAddScoped<IRobotsTxtService, DefaultRobotsTxtService>();
        services.TryAddScoped<IRobotsTxtTextService, DefaultRobotsTxtTextService>();

        return services;
    }
}
