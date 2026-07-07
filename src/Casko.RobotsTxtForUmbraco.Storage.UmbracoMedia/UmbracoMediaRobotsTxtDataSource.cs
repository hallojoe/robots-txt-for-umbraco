using System.Text;
using Casko.RobotsTxtForUmbraco.Storage;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtDataSource(
    IMediaService mediaService,
    IRobotsTxtStorageNameProvider nameProvider,
    IUmbracoMediaFileAccessor mediaFileAccessor) : IRobotsTxtDataSource
{
    public const string RootFolderName = "Robots Txt";
    private const int PageSize = 100;

    /// <inheritdoc />
    public Task<RobotsTxtStoredDocument?> ReadAsync(
        RobotsTxtStorageKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = nameProvider.GetFileName(key);
        var media = FindMedia(fileName);
        if (media is null)
        {
            return Task.FromResult<RobotsTxtStoredDocument?>(null);
        }

        var filePath = mediaFileAccessor.GetFilePath(media);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult<RobotsTxtStoredDocument?>(null);
        }

        using var stream = mediaFileAccessor.OpenRead(filePath);
        if (stream == Stream.Null)
        {
            return Task.FromResult<RobotsTxtStoredDocument?>(null);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();

        return Task.FromResult<RobotsTxtStoredDocument?>(CreateDocument(key, media, fileName, filePath, text));
    }

    /// <inheritdoc />
    public Task<RobotsTxtStoredDocument> WriteAsync(
        RobotsTxtStorageKey key,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = nameProvider.GetFileName(key);
        var folder = EnsureRootFolder();
        var media = FindMedia(fileName, folder.Id);

        if (media is not null)
        {
            var existingPath = mediaFileAccessor.GetFilePath(media);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                using var updateStream = CreateStream(text);
                mediaFileAccessor.UpdateFileContent(existingPath, updateStream);
                mediaService.Save(media);
                return Task.FromResult(CreateDocument(key, media, fileName, existingPath, text));
            }
        }

        media ??= mediaService.CreateMedia(fileName, folder, UmbracoConstants.Conventions.MediaTypes.File);

        using var createStream = CreateStream(text);
        mediaFileAccessor.SetInitialFile(media, fileName, createStream);
        mediaService.Save(media);

        return Task.FromResult(CreateDocument(key, media, fileName, mediaFileAccessor.GetFilePath(media), text));
    }

    private IMedia EnsureRootFolder()
    {
        var existing = mediaService.GetRootMedia()
            .FirstOrDefault(media => string.Equals(media.Name, RootFolderName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        var folder = mediaService.CreateMedia(
            RootFolderName,
            UmbracoConstants.System.Root,
            UmbracoConstants.Conventions.MediaTypes.Folder);
        mediaService.Save(folder);

        return folder;
    }

    private IMedia? FindMedia(string fileName, int? parentId = null)
    {
        var parent = parentId ?? mediaService.GetRootMedia()
            .FirstOrDefault(media => string.Equals(media.Name, RootFolderName, StringComparison.OrdinalIgnoreCase))
            ?.Id;

        if (parent is null)
        {
            return null;
        }

        long total;
        var pageIndex = 0;
        do
        {
            var children = mediaService.GetPagedChildren(parent.Value, pageIndex, PageSize, out total);
            var match = children.FirstOrDefault(media =>
                string.Equals(media.Name, fileName, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }

            pageIndex++;
        }
        while (pageIndex * PageSize < total);

        return null;
    }

    private static RobotsTxtStoredDocument CreateDocument(
        RobotsTxtStorageKey key,
        IMedia media,
        string fileName,
        string? mediaPath,
        string text)
    {
        return new RobotsTxtStoredDocument(
            key,
            media.Key,
            media.Id,
            fileName,
            mediaPath,
            text,
            GetRefreshedUtc(media));
    }

    private static MemoryStream CreateStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static DateTimeOffset? GetRefreshedUtc(IMedia media)
    {
        if (media.UpdateDate == default)
        {
            return null;
        }

        return media.UpdateDate.Kind switch
        {
            DateTimeKind.Local => new DateTimeOffset(media.UpdateDate).ToUniversalTime(),
            DateTimeKind.Utc => new DateTimeOffset(media.UpdateDate),
            _ => new DateTimeOffset(DateTime.SpecifyKind(media.UpdateDate, DateTimeKind.Utc))
        };
    }
}
