using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Storage;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class UmbracoMediaRobotsTxtRefreshBackgroundJobTests
{
    [Test]
    public async Task RunJobAsync_WhenEnabled_RefreshesAllConfiguredFiles()
    {
        var refreshService = Substitute.For<IRobotsTxtStorageRefreshService>();
        var logger = Substitute.For<ILogger<UmbracoMediaRobotsTxtRefreshBackgroundJob>>();
        var services = new ServiceCollection();
        services.AddScoped(_ => refreshService);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new UmbracoMediaRobotsTxtRefreshBackgroundJob(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RobotsTxtOptions
            {
                Storage = new RobotsTxtStorageOptions
                {
                    BackgroundJob = new RobotsTxtStorageBackgroundJobOptions
                    {
                        Enabled = true,
                        RefreshJobDelayInSeconds = 30
                    }
                }
            }),
            logger);

        await sut.RunJobAsync();

        await refreshService.Received(1).RefreshAllAsync();
        Assert.That(sut.Delay, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }
}
