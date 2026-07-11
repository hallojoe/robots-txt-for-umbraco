namespace Casko.RobotsTxtForUmbraco.Storage;

/// <summary>
/// Refreshes stored robots.txt documents.
/// </summary>
public interface IRobotsTxtStorageRefreshService
{
    /// <summary>
    /// Refreshes the stored document for a host.
    /// </summary>
    Task<string> RefreshAsync(string? hostName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes stored documents for all configured files.
    /// </summary>
    Task RefreshAllAsync(CancellationToken cancellationToken = default);
}
