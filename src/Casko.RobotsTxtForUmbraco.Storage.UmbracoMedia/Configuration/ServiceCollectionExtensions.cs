using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Storage.Configuration;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Extensions;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxtUmbracoMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var robotsTxtConfigurationSection = configuration.GetSection(RobotsTxtOptions.Key);
        var robotsTxtOptions = robotsTxtConfigurationSection.Get<RobotsTxtOptions>();

        if (robotsTxtOptions?.Enabled is not true)
        {
            return services;
        }
        
        services.AddRobotsTxtStorage(configuration);

        services.AddSingleton<IValidateOptions<MediaStorageOptions>, MediaStorageOptionsValidator>();
        services
            .AddOptions<MediaStorageOptions>()
            .Bind(configuration.GetSection(MediaStorageOptions.Key))
            .ValidateOnStart();
        
        services.AddScoped<IUmbracoMediaFileAccessor, UmbracoMediaFileAccessor>();
        services.AddScoped<IRobotsTxtDataSource, UmbracoMediaRobotsTxtDataSource>();
        
        services.AddScoped<IRobotsTxtTextService, StoredRobotsTxtTextService>();
        
        services.AddScoped<IRobotsTxtStorageRefreshService, RobotsTxtStorageRefreshService>();

        services.AddRecurringBackgroundJob<UmbracoMediaRobotsTxtRefreshBackgroundJob>();

        return services;
    }
}
