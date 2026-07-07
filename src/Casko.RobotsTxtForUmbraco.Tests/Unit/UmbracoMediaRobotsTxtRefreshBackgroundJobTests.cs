using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Storage;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;
using Microsoft.Extensions.DependencyInjection;
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
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var refreshService = Substitute.For<IRobotsTxtStorageRefreshService>();

        scope.ServiceProvider.Returns(serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        serviceProvider.GetService(typeof(IRobotsTxtStorageRefreshService)).Returns(refreshService);

        var sut = new UmbracoMediaRobotsTxtRefreshBackgroundJob(
            scopeFactory,
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
            }));

        await sut.RunJobAsync();

        await refreshService.Received(1).RefreshAllAsync();
        Assert.That(sut.Delay, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }
}
