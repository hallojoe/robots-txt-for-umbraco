using Casko.RobotsTxtForUmbraco.Models;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Rendering;

/// <summary>
/// Renders robots.txt models to their text representation.
/// </summary>
public interface IRobotsTxtRenderer
{
    /// <summary>
    /// Renders a robots.txt document.
    /// </summary>
    string Render(RobotsTxtDocument document);

    /// <summary>
    /// Parse a robots.txt document from string.
    /// </summary>
    RobotsTxtDocument Parse(string document);
    
    /// <summary>
    /// Merge two robots.txt documents.
    /// </summary>
    RobotsTxtDocument Merge(RobotsTxtDocument document1, RobotsTxtDocument document2);

    
}
