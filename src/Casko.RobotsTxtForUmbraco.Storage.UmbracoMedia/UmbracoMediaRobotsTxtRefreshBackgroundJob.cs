using System.Diagnostics;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtRefreshBackgroundJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RobotsTxtOptions> robotsTxtOptions,
    ILogger<UmbracoMediaRobotsTxtRefreshBackgroundJob> logger) : IRecurringBackgroundJob
{
    private const int DefaultIntervalSeconds = 3600;
    private const int MinimumDelaySeconds = 10;

    public TimeSpan Period => TimeSpan.FromSeconds(GetIntervalSeconds());

    public TimeSpan Delay => TimeSpan.FromSeconds(GetDelaySeconds());

    public ServerRole[] ServerRoles =>
    [
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
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting robots.txt media refresh background job at {StartedAt}.", startedAt);

        using var scope = serviceScopeFactory.CreateScope();
        var refreshService = scope.ServiceProvider.GetRequiredService<IRobotsTxtStorageRefreshService>();
        await refreshService.RefreshAllAsync();

        stopwatch.Stop();
        var completedAt = DateTimeOffset.UtcNow;
        logger.LogInformation(
            "Completed robots.txt media refresh background job at {CompletedAt}. Elapsed time: {ElapsedTime}.",
            completedAt,
            FormatElapsedTime(stopwatch.Elapsed));
    }

    private int GetIntervalSeconds()
    {
        var intervalSeconds = robotsTxtOptions.Value.Storage.BackgroundJob?.IntervalSeconds ?? DefaultIntervalSeconds;
        if (intervalSeconds > 0)
        {
            return intervalSeconds;
        }

        logger.LogDebug(
            "Configured robots.txt refresh interval {IntervalSeconds} is invalid. Using default interval {DefaultIntervalSeconds} seconds.",
            intervalSeconds,
            DefaultIntervalSeconds);
        return DefaultIntervalSeconds;
    }

    private int GetDelaySeconds()
    {
        var delaySeconds = robotsTxtOptions.Value.Storage.BackgroundJob?.RefreshJobDelayInSeconds ?? MinimumDelaySeconds;
        if (delaySeconds >= MinimumDelaySeconds)
        {
            return delaySeconds;
        }

        logger.LogDebug(
            "Configured robots.txt refresh delay {DelaySeconds} is below the minimum. Using {MinimumDelaySeconds} seconds.",
            delaySeconds,
            MinimumDelaySeconds);
        return MinimumDelaySeconds;
    }

    private static string FormatElapsedTime(TimeSpan elapsed) => elapsed.TotalHours >= 1
        ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m {elapsed.Seconds}s"
        : elapsed.TotalMinutes >= 1
            ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s"
            : $"{elapsed.TotalSeconds:F1}s";
}
