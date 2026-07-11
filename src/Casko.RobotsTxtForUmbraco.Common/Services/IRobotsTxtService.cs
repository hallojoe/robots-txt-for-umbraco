using Casko.RobotsTxtForUmbraco.Models;

namespace Casko.RobotsTxtForUmbraco.Common.Services;

/// <summary>
/// Generates robots.txt documents for incoming host contexts.
/// </summary>
public interface IRobotsTxtService
{
    /// <summary>
    /// Gets a robots.txt document for the supplied host name.
    /// </summary>
    Task<RobotsTxtDocument> GetAsync(string? hostName, CancellationToken cancellationToken = default);
}
