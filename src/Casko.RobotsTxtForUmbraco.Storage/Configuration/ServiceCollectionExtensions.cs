using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Casko.RobotsTxtForUmbraco.Storage.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxtStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var robotsTxtConfigurationSection = configuration.GetSection(RobotsTxtOptions.Key);
        var robotsTxtOptions = robotsTxtConfigurationSection.Get<RobotsTxtOptions>();

        if (robotsTxtOptions?.Enabled is not true)
        {
            return services;
        }
        
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IRobotsTxtStorageNameProvider, RobotsTxtStorageNameProvider>();
        services.AddScoped<IRobotsTxtStorageRefreshService, RobotsTxtStorageRefreshService>();
        services.Replace(ServiceDescriptor.Scoped<IRobotsTxtTextService, StoredRobotsTxtTextService>());
        return services;
    }
}
