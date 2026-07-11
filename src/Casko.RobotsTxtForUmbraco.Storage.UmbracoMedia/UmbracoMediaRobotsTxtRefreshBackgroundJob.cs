using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtRefreshBackgroundJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RobotsTxtOptions> options, 
    ILogger<UmbracoMediaRobotsTxtRefreshBackgroundJob> logger) : IRecurringBackgroundJob
{
    public TimeSpan Period => TimeSpan.FromSeconds(GetIntervalSeconds());

    public TimeSpan Delay => TimeSpan.FromSeconds(GetDelaySeconds());

    /// <summary>
    /// Do not run on <cref name="ServerRole.Subscriber">Subscriber</cref>.
    /// </summary>
    public ServerRole[] ServerRoles => [
        ServerRole.SchedulingPublisher, 
        ServerRole.Single, 
        ServerRole.Unknown
    ];

    public event EventHandler? PeriodChanged
    {
        add { }
        remove { }
    }

    public async Task RunJobAsync()
    {
        if ((options.Value is { Enabled: true, Storage.BackgroundJob.Enabled: true }) is not true)
        {
            logger.LogInformation(
                "Background job for Umbraco Media `robots.txt` storage is disabled. `{Job}.{Caller}` gracefully exited.", 
                nameof(UmbracoMediaRobotsTxtRefreshBackgroundJob), 
                nameof(RunJobAsync));

            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var refreshService = scope.ServiceProvider.GetRequiredService<IRobotsTxtStorageRefreshService>();
            
            logger.LogInformation(
                "Background job for Umbraco Media `robots.txt` storage refreshing. `{Job}.{Caller}` starting.", 
                nameof(UmbracoMediaRobotsTxtRefreshBackgroundJob), 
                nameof(RunJobAsync));
            
            await refreshService.RefreshAllAsync();
        
            logger.LogInformation(
                "Background job for Umbraco Media `robots.txt` storage refreshed. `{Job}.{Caller}` completed.", 
                nameof(UmbracoMediaRobotsTxtRefreshBackgroundJob), 
                nameof(RunJobAsync));

        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Background job for Umbraco Media `robots.txt` storage failed. `{Job}.{Caller}` failed.", 
                nameof(UmbracoMediaRobotsTxtRefreshBackgroundJob), 
                nameof(RunJobAsync));
        }
    }
                    
    private int GetIntervalSeconds()
    {
        var intervalSeconds = options.Value.Storage.BackgroundJob.IntervalSeconds;
        return intervalSeconds > 0 ? intervalSeconds : 3600;
    }

    private int GetDelaySeconds()
    {
        var delaySeconds = options.Value.Storage.BackgroundJob.RefreshJobDelayInSeconds;
        return delaySeconds > 0 ? delaySeconds : 10;
    }
}
