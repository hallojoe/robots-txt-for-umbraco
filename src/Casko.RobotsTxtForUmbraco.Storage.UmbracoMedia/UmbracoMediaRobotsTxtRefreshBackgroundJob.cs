using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtRefreshBackgroundJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RobotsTxtOptions> options) : IRecurringBackgroundJob
{
    public TimeSpan Period => TimeSpan.FromSeconds(GetIntervalSeconds());

    public TimeSpan Delay => TimeSpan.FromSeconds(GetDelaySeconds());

    public ServerRole[] ServerRoles => IRecurringBackgroundJob.DefaultServerRoles;

    public event EventHandler? PeriodChanged
    {
        add { }
        remove { }
    }

    public async Task RunJobAsync()
    {
        if (!options.Value.Storage.BackgroundJob.Enabled)
        {
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var refreshService = scope.ServiceProvider.GetRequiredService<IRobotsTxtStorageRefreshService>();
        await refreshService.RefreshAllAsync();
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
