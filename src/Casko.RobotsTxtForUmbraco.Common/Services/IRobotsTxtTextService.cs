namespace Casko.RobotsTxtForUmbraco.Common.Services;

/// <summary>
/// Generates rendered robots.txt text for incoming host contexts.
/// </summary>
public interface IRobotsTxtTextService
{
    /// <summary>
    /// Gets rendered robots.txt text for the supplied host name.
    /// </summary>
    Task<string> GetTextAsync(string? hostName, CancellationToken cancellationToken = default);
}
