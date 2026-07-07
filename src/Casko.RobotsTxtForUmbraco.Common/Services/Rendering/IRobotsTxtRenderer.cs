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
}
