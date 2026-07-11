using Umbraco.Cms.Core.Models;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia;

public interface IUmbracoMediaFileAccessor
{
    string? GetFilePath(IMedia media);

    Stream OpenRead(string filePath);

    void SetInitialFile(IMedia media, string fileName, Stream content);

    void UpdateFileContent(string filePath, Stream content);
}
