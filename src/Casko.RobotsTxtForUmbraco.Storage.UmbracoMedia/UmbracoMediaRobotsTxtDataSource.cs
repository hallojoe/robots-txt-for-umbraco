using System.Text;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtDataSource(
    IOptions<MediaStorageOptions> mediaStorageOptions,
    IOptions<RobotsTxtOptions> robotsTxtOptions,
    IRobotsTxtRenderer robotsTxtRenderer,
    IMediaService mediaService,
    IRobotsTxtStorageNameProvider nameProvider,
    IUmbracoMediaFileAccessor mediaFileAccessor,
    HybridCache cache,
    TimeProvider timeProvider) : IRobotsTxtDataSource
{
    private const int PageSize = 100;
    private const int RetainedVersionCount = 2;
    private static readonly HybridCacheEntryOptions CacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public async Task<RobotsTxtStoredDocument?> ReadAsync(RobotsTxtStorageKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var logicalFileName = nameProvider.GetFileName(key);
        var cacheKey = GetCacheKey(key);
        var version = await cache.GetOrCreateAsync(cacheKey, _ => ValueTask.FromResult(ResolveLatestVersion(logicalFileName)), CacheEntryOptions, cancellationToken: cancellationToken);
        var document = version is null ? null : ReadVersion(key, version);
        if (document is not null || version is null)
        {
            return document;
        }

        await cache.RemoveAsync(cacheKey, cancellationToken);
        var fallback = ResolveLatestVersion(logicalFileName, version.MediaKey);
        if (fallback is null)
        {
            return null;
        }

        await cache.SetAsync(cacheKey, fallback, CacheEntryOptions, cancellationToken: cancellationToken);
        return ReadVersion(key, fallback);
    }

    public async Task<RobotsTxtStoredDocument> WriteAsync(RobotsTxtStorageKey key, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var logicalFileName = nameProvider.GetFileName(key);
        var currentMedia = ResolveLatestMedia(logicalFileName);
        var customization = currentMedia is null ? null : ReadRobotsTxtCustomization(currentMedia, mediaStorageOptions.Value);
        text = MergeRobotsTxtFileContents(customization, text) ?? text;

        var folder = EnsureRootFolder();
        var versionedFileName = CreateVersionedFileName(logicalFileName, timeProvider.GetUtcNow());
        var media = mediaService.CreateMedia(versionedFileName, folder, mediaStorageOptions.Value.MediaTypeAlias);
        CopyRobotsTxtCustomization(media, customization, mediaStorageOptions.Value);
        using var createStream = CreateStream(text);
        mediaFileAccessor.SetInitialFile(media, versionedFileName, createStream);
        mediaService.Save(media);

        var mediaPath = mediaFileAccessor.GetFilePath(media);
        var version = CreateVersion(media, versionedFileName, mediaPath);
        await cache.SetAsync(GetCacheKey(key), version, CacheEntryOptions, cancellationToken: cancellationToken);
        CleanupObsoleteVersions(folder.Id, logicalFileName);
        return CreateDocument(key, media, versionedFileName, mediaPath, text);
    }

    private RobotsTxtStoredDocument? ReadVersion(RobotsTxtStorageKey key, StoredRobotsTxtMediaVersion version)
    {
        using var stream = mediaFileAccessor.OpenRead(version.MediaPath);
        if (stream == Stream.Null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return new RobotsTxtStoredDocument(key, version.MediaKey, version.MediaId, version.FileName, version.MediaPath, reader.ReadToEnd(), version.PublishedUtc);
    }

    private StoredRobotsTxtMediaVersion? ResolveLatestVersion(string logicalFileName, Guid? excludedMediaKey = null)
    {
        var candidate = ResolveLatestMedia(logicalFileName, excludedMediaKey);
        if (candidate is null) return null;
        var path = mediaFileAccessor.GetFilePath(candidate);
        return string.IsNullOrWhiteSpace(path) ? null : CreateVersion(candidate, candidate.Name ?? logicalFileName, path);
    }

    private IMedia? ResolveLatestMedia(string logicalFileName, Guid? excludedMediaKey = null)
    {
        var folder = FindRootFolder();
        if (folder is null) return null;
        var children = GetChildren(folder.Id).Where(media => media.Key != excludedMediaKey).ToArray();
        var candidates = children.Where(media => IsVersionOf(media.Name ?? string.Empty, logicalFileName)).OrderByDescending(media => media.Name, StringComparer.Ordinal).ToArray();
        if (candidates.Length == 0)
        {
            candidates = children.Where(media => string.Equals(media.Name, logicalFileName, StringComparison.OrdinalIgnoreCase)).OrderByDescending(GetRefreshedUtc).ToArray();
        }

        return candidates.FirstOrDefault(media => !string.IsNullOrWhiteSpace(mediaFileAccessor.GetFilePath(media)));
    }

    private void CleanupObsoleteVersions(int folderId, string logicalFileName)
    {
        var cleanupAfterSeconds = robotsTxtOptions.Value.Storage.VersionCleanupAfterSeconds;
        if (cleanupAfterSeconds <= 0)
        {
            return;
        }

        var cutoff = timeProvider.GetUtcNow().AddSeconds(-cleanupAfterSeconds);
        var obsoleteVersions = GetChildren(folderId)
            .Where(media => IsVersionOf(media.Name ?? string.Empty, logicalFileName))
            .OrderByDescending(media => media.Name, StringComparer.Ordinal)
            .Skip(RetainedVersionCount)
            .Where(media => GetRefreshedUtc(media) is { } refreshedUtc && refreshedUtc <= cutoff);
        foreach (var media in obsoleteVersions)
        {
            mediaService.Delete(media);
        }
    }

    private static bool HasRobotsTxtCustomizations(IMedia media, MediaStorageOptions options) =>
        !string.IsNullOrWhiteSpace(options.MediaTypeAlias) &&
        !string.IsNullOrWhiteSpace(options.MediaTypePropertyAlias) &&
        media.HasProperty(options.MediaTypePropertyAlias);

    private static string? ReadRobotsTxtCustomization(IMedia media, MediaStorageOptions options)
    {
        if (!HasRobotsTxtCustomizations(media, options))
        {
            return null;
        }

        var customization = media.GetValue<string?>(options.MediaTypePropertyAlias!);
        return string.IsNullOrWhiteSpace(customization) ? null : customization;
    }

    private static void CopyRobotsTxtCustomization(IMedia media, string? customization, MediaStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(customization) && HasRobotsTxtCustomizations(media, options))
        {
            media.SetValue(options.MediaTypePropertyAlias!, customization);
        }
    }

    private string? MergeRobotsTxtFileContents(string? customization, string? generatedText)
    {
        if (string.IsNullOrWhiteSpace(generatedText)) return customization;
        if (string.IsNullOrWhiteSpace(customization)) return generatedText;
        var customDocument = robotsTxtRenderer.Parse(customization);
        var generatedDocument = robotsTxtRenderer.Parse(generatedText);
        return robotsTxtRenderer.Render(robotsTxtRenderer.Merge(generatedDocument, customDocument));
    }

    private IMedia EnsureRootFolder()
    {
        var existing = FindRootFolder();
        if (existing is not null) return existing;
        var folder = mediaService.CreateMedia(mediaStorageOptions.Value.FolderName, UmbracoConstants.System.Root, UmbracoConstants.Conventions.MediaTypes.Folder);
        mediaService.Save(folder);
        return folder;
    }

    private IMedia? FindRootFolder() => mediaService.GetRootMedia().FirstOrDefault(media => string.Equals(media.Name, mediaStorageOptions.Value.FolderName, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<IMedia> GetChildren(int parentId)
    {
        long total;
        var pageIndex = 0;
        do
        {
            var children = mediaService.GetPagedChildren(parentId, pageIndex, PageSize, out total);
            foreach (var child in children) yield return child;
            pageIndex++;
        } while (pageIndex * PageSize < total);
    }

    private static bool IsVersionOf(string candidateFileName, string logicalFileName)
    {
        var extension = Path.GetExtension(logicalFileName);
        var stem = Path.GetFileNameWithoutExtension(logicalFileName);
        return candidateFileName.StartsWith($"{stem}--", StringComparison.OrdinalIgnoreCase) && candidateFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateVersionedFileName(string logicalFileName, DateTimeOffset publishedUtc)
    {
        var extension = Path.GetExtension(logicalFileName);
        var stem = Path.GetFileNameWithoutExtension(logicalFileName);
        return $"{stem}--{publishedUtc:yyyyMMddHHmmssfffffff}Z--{Guid.NewGuid():N}{extension}";
    }

    private static string GetCacheKey(RobotsTxtStorageKey key) => $"robots-txt:media-version:{key.NormalizedHostName}";

    private static StoredRobotsTxtMediaVersion CreateVersion(IMedia media, string fileName, string? mediaPath) =>
        new(media.Key, media.Id, fileName, mediaPath ?? string.Empty, GetRefreshedUtc(media));

    private static RobotsTxtStoredDocument CreateDocument(RobotsTxtStorageKey key, IMedia media, string fileName, string? mediaPath, string text) =>
        new(key, media.Key, media.Id, fileName, mediaPath, text, GetRefreshedUtc(media));

    private static MemoryStream CreateStream(string text) => new(Encoding.UTF8.GetBytes(text));

    private static DateTimeOffset? GetRefreshedUtc(IMedia media)
    {
        if (media.UpdateDate == default) return null;
        return media.UpdateDate.Kind switch
        {
            DateTimeKind.Local => new DateTimeOffset(media.UpdateDate).ToUniversalTime(),
            DateTimeKind.Utc => new DateTimeOffset(media.UpdateDate),
            _ => new DateTimeOffset(DateTime.SpecifyKind(media.UpdateDate, DateTimeKind.Utc))
        };
    }

    private sealed record StoredRobotsTxtMediaVersion(Guid MediaKey, int MediaId, string FileName, string MediaPath, DateTimeOffset? PublishedUtc);
}
