namespace Casko.RobotsTxtForUmbraco.Storage;

/// <summary>
/// Stores rendered robots.txt documents.
/// </summary>
public interface IRobotsTxtDataSource
{
    /// <summary>
    /// Reads a stored robots.txt document.
    /// </summary>
    Task<RobotsTxtStoredDocument?> ReadAsync(RobotsTxtStorageKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a stored robots.txt document.
    /// </summary>
    Task<RobotsTxtStoredDocument> WriteAsync(RobotsTxtStorageKey key, string text, CancellationToken cancellationToken = default);
}
