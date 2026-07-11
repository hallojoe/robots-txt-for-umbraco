using System.Text;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaRobotsTxtDataSource(
    IOptions<MediaStorageOptions> mediaStorageOptions,
    IRobotsTxtRenderer robotsTxtRenderer,
    IMediaService mediaService,
    IRobotsTxtStorageNameProvider nameProvider,
    IUmbracoMediaFileAccessor mediaFileAccessor) : IRobotsTxtDataSource
{
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
        
        var createdDocument = CreateDocument(key, media, fileName, filePath, text);
        
        return Task.FromResult<RobotsTxtStoredDocument?>(createdDocument);
    }

    private static bool HasRobotsTxtCustomizations(IMedia media, MediaStorageOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.MediaTypeAlias) &&
               !string.IsNullOrWhiteSpace(options.MediaTypePropertyAlias) &&
               media.HasProperty(options.MediaTypePropertyAlias);
    }

    private static string? ReadRobotsTxtCustomization(IMedia media, MediaStorageOptions options)
    {
        if (!HasRobotsTxtCustomizations(media, options))
        {
            return null;
        }

        var userRobotsText = media.GetValue<string?>(options.MediaTypePropertyAlias!);
        
        return string.IsNullOrWhiteSpace(userRobotsText) ? null : userRobotsText;
    }

    private string? MergeRobotsTxtFileContents(string? userRobotsTxtString, string? existingRobotsTxtString)
    {
        if (string.IsNullOrWhiteSpace(existingRobotsTxtString))
        {
            return userRobotsTxtString;
        }

        if (string.IsNullOrWhiteSpace(userRobotsTxtString))
        {
            return existingRobotsTxtString;
        }
        
        var userRobotsTextDocument = robotsTxtRenderer.Parse(userRobotsTxtString);
        var existingRobotsTextDocument = robotsTxtRenderer.Parse(existingRobotsTxtString);
        var mergedRobotsTextDocument = robotsTxtRenderer.Merge(existingRobotsTextDocument, userRobotsTextDocument);
        var reRenderedRobotsText = robotsTxtRenderer.Render(mergedRobotsTextDocument);
        
        return reRenderedRobotsText;
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
                var userRobotsText = ReadRobotsTxtCustomization(media, mediaStorageOptions.Value);
                var userRobotsTextMerged = MergeRobotsTxtFileContents(userRobotsText, text);
                if (userRobotsTextMerged is not null)
                {
                    text = userRobotsTextMerged;
                }

                using var updateStream = CreateStream(text);

                mediaFileAccessor.UpdateFileContent(existingPath, updateStream);
                
                mediaService.Save(media);

                var createdDocument = CreateDocument(key, media, fileName, existingPath, text);
                
                return Task.FromResult(createdDocument);
            }
        }

        media ??= mediaService.CreateMedia(fileName, folder, mediaStorageOptions.Value.MediaTypeAlias);

        using var createStream = CreateStream(text);
        mediaFileAccessor.SetInitialFile(media, fileName, createStream);
        mediaService.Save(media);

        return Task.FromResult(CreateDocument(key, media, fileName, mediaFileAccessor.GetFilePath(media), text));
    }

    private IMedia EnsureRootFolder()
    {
        var existing = mediaService.GetRootMedia()
            .FirstOrDefault(media => string.Equals(media.Name, mediaStorageOptions.Value.FolderName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        var folder = mediaService.CreateMedia(
            mediaStorageOptions.Value.FolderName,
            UmbracoConstants.System.Root,
            UmbracoConstants.Conventions.MediaTypes.Folder);
        mediaService.Save(folder);

        return folder;
    }

    private IMedia? FindMedia(string fileName, int? parentId = null)
    {
        var parent = parentId ?? mediaService.GetRootMedia()
            .FirstOrDefault(media => string.Equals(
                media.Name, mediaStorageOptions.Value.FolderName, StringComparison.OrdinalIgnoreCase))
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
