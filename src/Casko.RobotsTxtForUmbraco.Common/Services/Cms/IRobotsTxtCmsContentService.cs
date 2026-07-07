using Umbraco.Cms.Core.Models.PublishedContent;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Cms;

/// <summary>
/// Provides the published content and languages needed by robots.txt generation.
/// </summary>
public interface IRobotsTxtCmsContentService
{
    /// <summary>
    /// Gets root content nodes for the current published snapshot.
    /// </summary>
    IEnumerable<IPublishedContent> GetRootContents(string? hostName = null);

    /// <summary>
    /// Gets normalized disallow paths for the current published snapshot.
    /// </summary>
    IEnumerable<string> GetDisallowedContents(string? hostName = null);
    
    /// <summary>
    /// Gets language ISO codes with the default language first.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetLanguagesAsync();
}
