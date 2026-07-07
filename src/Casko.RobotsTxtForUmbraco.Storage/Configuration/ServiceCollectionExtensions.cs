using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Casko.RobotsTxtForUmbraco.Storage.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxtStorage(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IRobotsTxtStorageNameProvider, RobotsTxtStorageNameProvider>();
        services.AddScoped<IRobotsTxtStorageRefreshService, RobotsTxtStorageRefreshService>();
        services.Replace(ServiceDescriptor.Scoped<IRobotsTxtTextService, StoredRobotsTxtTextService>());
        return services;
    }
}
