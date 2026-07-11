using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Casko.RobotsTxtForUmbraco.Common.Services;

/// <summary>
/// Collects content considered by robots.txt generation.
/// </summary>
// public interface IRobotsTxtContentCollector
// {
//     /// <summary>
//     /// Collects each root and its descendants.
//     /// </summary>
//     IEnumerable<IPublishedContent> Collect(IEnumerable<IPublishedContent> rootContents);
// }
//
// public sealed class RobotsTxtContentCollector : IRobotsTxtContentCollector
// {
//     /// <inheritdoc />
//     public IEnumerable<IPublishedContent> Collect(IEnumerable<IPublishedContent> rootContents)
//     {
//         return rootContents.SelectMany(root => root.Descendants().Prepend(root));
//     }
// }
