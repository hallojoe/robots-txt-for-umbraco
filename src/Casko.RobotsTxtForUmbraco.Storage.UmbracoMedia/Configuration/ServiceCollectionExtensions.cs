using Casko.RobotsTxtForUmbraco.Storage.Configuration;
using Casko.RobotsTxtForUmbraco.Storage;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxtUmbracoMediaStorage(this IServiceCollection services)
    {
        services.AddRobotsTxtStorage();
        services.AddScoped<IUmbracoMediaFileAccessor, UmbracoMediaFileAccessor>();
        services.AddScoped<IRobotsTxtDataSource, UmbracoMediaRobotsTxtDataSource>();
        services.AddSingleton<IRecurringBackgroundJob, UmbracoMediaRobotsTxtRefreshBackgroundJob>();
        return services;
    }
}
