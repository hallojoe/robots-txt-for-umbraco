using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Storage;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class StoredRobotsTxtTextServiceTests
{
    [Test]
    public async Task GetTextAsync_ReturnsFreshStoredDocument()
    {
        var dataSource = Substitute.For<IRobotsTxtDataSource>();
        var refreshService = Substitute.For<IRobotsTxtStorageRefreshService>();
        var storedDocument = new RobotsTxtStoredDocument(
            new RobotsTxtStorageKey("example.com"),
            Guid.NewGuid(),
            1,
            "robots-example.com.txt",
            "/media/robots-example.com.txt",
            "User-agent: *",
            DateTimeOffset.UtcNow);

        dataSource.ReadAsync(Arg.Any<RobotsTxtStorageKey>(), Arg.Any<CancellationToken>())
            .Returns(storedDocument);

        var service = CreateService(dataSource, refreshService);

        var result = await service.GetTextAsync("example.com");

        Assert.That(result, Is.EqualTo("User-agent: *"));
        await refreshService.DidNotReceiveWithAnyArgs().RefreshAsync(default);
    }

    [Test]
    public async Task GetTextAsync_RefreshesStaleStoredDocument()
    {
        var dataSource = Substitute.For<IRobotsTxtDataSource>();
        var refreshService = Substitute.For<IRobotsTxtStorageRefreshService>();
        var storedDocument = new RobotsTxtStoredDocument(
            new RobotsTxtStorageKey("example.com"),
            Guid.NewGuid(),
            1,
            "robots-example.com.txt",
            "/media/robots-example.com.txt",
            "stale",
            DateTimeOffset.UtcNow.AddHours(-2));

        dataSource.ReadAsync(Arg.Any<RobotsTxtStorageKey>(), Arg.Any<CancellationToken>())
            .Returns(storedDocument);
        refreshService.RefreshAsync("example.com", Arg.Any<CancellationToken>())
            .Returns("fresh");

        var service = CreateService(dataSource, refreshService);

        var result = await service.GetTextAsync("example.com");

        Assert.That(result, Is.EqualTo("fresh"));
    }

    private static StoredRobotsTxtTextService CreateService(
        IRobotsTxtDataSource dataSource,
        IRobotsTxtStorageRefreshService refreshService)
    {
        return new StoredRobotsTxtTextService(
            dataSource,
            refreshService,
            Options.Create(new RobotsTxtOptions
            {
                Storage = new RobotsTxtStorageOptions
                {
                    RefreshStaleAfterSeconds = 3600
                }
            }),
            TimeProvider.System);
    }
}
