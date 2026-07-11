namespace Casko.RobotsTxtForUmbraco.Storage;

/// <summary>
/// Resolves stable storage file names for robots.txt documents.
/// </summary>
public interface IRobotsTxtStorageNameProvider
{
    /// <summary>
    /// Gets the file name for a storage key.
    /// </summary>
    string GetFileName(RobotsTxtStorageKey key);
}
