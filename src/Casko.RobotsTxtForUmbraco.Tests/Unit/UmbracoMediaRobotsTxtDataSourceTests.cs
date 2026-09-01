using System.Text;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Casko.RobotsTxtForUmbraco.Storage;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class UmbracoMediaRobotsTxtDataSourceTests
{
    private IMediaService _mediaService = null!;
    private IUmbracoMediaFileAccessor _mediaFileAccessor = null!;
    private ServiceProvider _serviceProvider = null!;
    private UmbracoMediaRobotsTxtDataSource _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mediaService = Substitute.For<IMediaService>();
        _mediaFileAccessor = Substitute.For<IUmbracoMediaFileAccessor>();
        var services = new ServiceCollection();
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();
        _sut = CreateSut();
    }

    [TearDown]
    public void TearDown() => _serviceProvider.Dispose();

    [Test]
    public async Task ReadAsync_WhenLegacyFileExists_ReturnsStoredText()
    {
        var folder = CreateMedia(10, "robots.txt");
        var legacy = CreateMedia(20, "robots-www.example.com.txt");
        ConfigureRootFolder(folder);
        ConfigureChildren(folder, [legacy]);
        _mediaFileAccessor.GetFilePath(legacy).Returns("/media/robots.txt");
        _mediaFileAccessor.OpenRead("/media/robots.txt").Returns(new MemoryStream(Encoding.UTF8.GetBytes("legacy")));

        var result = await _sut.ReadAsync(CreateKey());

        Assert.That(result?.Text, Is.EqualTo("legacy"));
        Assert.That(result?.MediaId, Is.EqualTo(20));
    }

    [Test]
    public async Task ReadAsync_WhenVersionedFilesExist_ReturnsLatestVersion()
    {
        var folder = CreateMedia(10, "robots.txt");
        var older = CreateMedia(20, "robots-www.example.com--202608181000000000000Z--a.txt");
        var latest = CreateMedia(21, "robots-www.example.com--202608181100000000000Z--b.txt");
        ConfigureRootFolder(folder);
        ConfigureChildren(folder, [older, latest]);
        _mediaFileAccessor.GetFilePath(latest).Returns("/media/latest.txt");
        _mediaFileAccessor.OpenRead("/media/latest.txt").Returns(new MemoryStream(Encoding.UTF8.GetBytes("latest")));

        var result = await _sut.ReadAsync(CreateKey());

        Assert.That(result?.Text, Is.EqualTo("latest"));
        Assert.That(result?.MediaId, Is.EqualTo(21));
    }

    [Test]
    public async Task ReadAsync_WhenLatestVersionCannotBeRead_FallsBackToPreviousVersion()
    {
        var folder = CreateMedia(10, "robots.txt");
        var older = CreateMedia(20, "robots-www.example.com--202608181000000000000Z--a.txt");
        var latest = CreateMedia(21, "robots-www.example.com--202608181100000000000Z--b.txt");
        ConfigureRootFolder(folder);
        ConfigureChildren(folder, [older, latest]);
        _mediaFileAccessor.GetFilePath(older).Returns("/media/older.txt");
        _mediaFileAccessor.GetFilePath(latest).Returns("/media/latest.txt");
        _mediaFileAccessor.OpenRead("/media/latest.txt").Returns(Stream.Null);
        _mediaFileAccessor.OpenRead("/media/older.txt").Returns(new MemoryStream(Encoding.UTF8.GetBytes("older")));

        var result = await _sut.ReadAsync(CreateKey());

        Assert.That(result?.Text, Is.EqualTo("older"));
        Assert.That(result?.MediaId, Is.EqualTo(20));
    }

    [Test]
    public async Task WriteAsync_CreatesImmutableVersion()
    {
        var folder = CreateMedia(10, "robots.txt");
        var created = CreateMedia(20, "created.txt");
        ConfigureRootFolder(folder);
        ConfigureChildren(folder, []);
        _mediaService.CreateMedia(
                Arg.Is<string>(name => name.StartsWith("robots-www.example.com--", StringComparison.Ordinal)),
                folder,
                "file")
            .Returns(created);
        _mediaFileAccessor.GetFilePath(created).Returns("/media/created.txt");

        var result = await _sut.WriteAsync(CreateKey(), "generated");

        Assert.That(result.FileName, Does.StartWith("robots-www.example.com--"));
        _mediaFileAccessor.Received(1).SetInitialFile(created, Arg.Any<string>(), Arg.Any<Stream>());
        _mediaService.Received(1).Save(created);
    }

    [Test]
    public async Task WriteAsync_CleansUpExpiredVersionsBeyondTheTwoNewest()
    {
        var folder = CreateMedia(10, "robots.txt");
        var first = CreateMedia(20, "robots-www.example.com--202608181200000000000Z--a.txt", DateTime.UtcNow);
        var second = CreateMedia(21, "robots-www.example.com--202608181100000000000Z--b.txt", DateTime.UtcNow);
        var expired = CreateMedia(22, "robots-www.example.com--202608180900000000000Z--c.txt", DateTime.UtcNow.AddHours(-1));
        var created = CreateMedia(23, "robots-www.example.com--999912312359599999999Z--d.txt");
        ConfigureRootFolder(folder);
        ConfigureChildren(folder, [created, first, second, expired]);
        _mediaService.CreateMedia(Arg.Any<string>(), folder, "file").Returns(created);
        _mediaFileAccessor.GetFilePath(created).Returns("/media/created.txt");
        _sut = CreateSut(new RobotsTxtOptions { Storage = new RobotsTxtStorageOptions { VersionCleanupAfterSeconds = 600 } });

        await _sut.WriteAsync(CreateKey(), "generated");

        _mediaService.Received(1).Delete(expired);
        _mediaService.DidNotReceive().Delete(first);
        _mediaService.DidNotReceive().Delete(second);
    }

    [Test]
    public void WriteAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        AsyncTestDelegate action = async () => await _sut.WriteAsync(CreateKey(), "generated", cancellationTokenSource.Token);

        Assert.That(action, Throws.TypeOf<OperationCanceledException>());
        _mediaService.DidNotReceive().GetRootMedia();
    }

    private UmbracoMediaRobotsTxtDataSource CreateSut(RobotsTxtOptions? robotsTxtOptions = null)
    {
        return new UmbracoMediaRobotsTxtDataSource(
            Options.Create(new MediaStorageOptions()),
            Options.Create(robotsTxtOptions ?? new RobotsTxtOptions()),
            new RobotsTxtRenderer(),
            _mediaService,
            new RobotsTxtStorageNameProvider(),
            _mediaFileAccessor,
            _serviceProvider.GetRequiredService<HybridCache>(),
            TimeProvider.System);
    }

    private static RobotsTxtStorageKey CreateKey() => new("www.example.com");

    private void ConfigureRootFolder(IMedia folder) => _mediaService.GetRootMedia().Returns([folder]);

    private void ConfigureChildren(IMedia folder, IEnumerable<IMedia> children)
    {
        _mediaService.GetPagedChildren(folder.Id, 0, 100, out Arg.Any<long>())
            .Returns(callInfo =>
            {
                var childList = children.ToList();
                callInfo[3] = (long)childList.Count;
                return childList;
            });
    }

    private static IMedia CreateMedia(int id, string name, DateTime? updateDate = null)
    {
        var media = Substitute.For<IMedia>();
        media.Id.Returns(id);
        media.Key.Returns(Guid.NewGuid());
        media.Name.Returns(name);
        media.UpdateDate.Returns(updateDate ?? DateTime.UtcNow);
        return media;
    }
}
