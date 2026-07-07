using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Models;
using Casko.RobotsTxtForUmbraco.Storage;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtStorageRefreshServiceTests
{
    [Test]
    public async Task RefreshAllAsync_RefreshesAllConfiguredHostsAndDefault()
    {
        var robotsTxtService = Substitute.For<IRobotsTxtService>();
        var renderer = Substitute.For<Casko.RobotsTxtForUmbraco.Common.Services.Rendering.IRobotsTxtRenderer>();
        var dataSource = Substitute.For<IRobotsTxtDataSource>();
        robotsTxtService.GetAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new RobotsTxtDocument());
        renderer.Render(Arg.Any<RobotsTxtDocument>()).Returns("robots");

        var sut = new RobotsTxtStorageRefreshService(
            robotsTxtService,
            renderer,
            dataSource,
            Options.Create(new RobotsTxtOptions
            {
                Files = new Dictionary<string, RobotsTxtFileOptions>
                {
                    ["default"] = new() { HostName = null },
                    ["host-1"] = new() { HostName = "example.com" },
                    ["host-2"] = new() { HostName = "example.org" }
                }
            }));

        await sut.RefreshAllAsync();

        await robotsTxtService.Received(1).GetAsync((string?)null, Arg.Any<CancellationToken>());
        await robotsTxtService.Received(1).GetAsync("example.com", Arg.Any<CancellationToken>());
        await robotsTxtService.Received(1).GetAsync("example.org", Arg.Any<CancellationToken>());
        await dataSource.Received(1).WriteAsync(Arg.Is<RobotsTxtStorageKey>(key => key.HostName == null), "robots", Arg.Any<CancellationToken>());
    }
}
