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
    public async Task RefreshAllAsync_RefreshesConfiguredBindingsAndDefault()
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
                Hosts = new Dictionary<string, RobotsTxtBindingOptions>
                {
                    [""] = new() { Include = ["robots.default.txt"] },
                    ["binding-1"] = new() { Host = "example.com", Include = ["robots.host-1.txt"] },
                    ["binding-2"] = new() { Host = "example.org", Include = ["robots.host-2.txt"] }
                }
            }));

        await sut.RefreshAllAsync();

        await robotsTxtService.Received(1).GetAsync((string?)null, Arg.Any<CancellationToken>());
        await robotsTxtService.Received(1).GetAsync("example.com", Arg.Any<CancellationToken>());
        await robotsTxtService.Received(1).GetAsync("example.org", Arg.Any<CancellationToken>());
        await dataSource.Received(1).WriteAsync(
            Arg.Is<RobotsTxtStorageKey>(key => key.HostName == null),
            "robots",
            Arg.Any<CancellationToken>());
    }
}
