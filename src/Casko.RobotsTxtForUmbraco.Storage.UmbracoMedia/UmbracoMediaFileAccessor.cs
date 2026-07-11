using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public sealed class UmbracoMediaFileAccessor(
    IMediaService mediaService,
    MediaFileManager mediaFileManager,
    MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
    IShortStringHelper shortStringHelper,
    IContentTypeBaseServiceProvider contentTypeBaseServiceProvider) : IUmbracoMediaFileAccessor
{
    public string? GetFilePath(IMedia media)
    {
        return media.GetValue<string>(UmbracoConstants.Conventions.Media.File);
    }

    public Stream OpenRead(string filePath)
    {
        return mediaService.GetMediaFileContentStream(filePath);
    }

    public void SetInitialFile(IMedia media, string fileName, Stream content)
    {
        media.SetValue(
            mediaFileManager,
            mediaUrlGeneratorCollection,
            shortStringHelper,
            contentTypeBaseServiceProvider,
            UmbracoConstants.Conventions.Media.File,
            fileName,
            content);
    }

    public void UpdateFileContent(string filePath, Stream content)
    {
        mediaService.SetMediaFileContent(filePath, content);
    }
}
