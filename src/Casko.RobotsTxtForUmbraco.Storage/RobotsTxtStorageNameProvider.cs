using System.Text.RegularExpressions;

namespace Casko.RobotsTxtForUmbraco.Storage;

public sealed partial class RobotsTxtStorageNameProvider : IRobotsTxtStorageNameProvider
{
    public string GetFileName(RobotsTxtStorageKey key)
    {
        var host = InvalidFileNameCharacters().Replace(key.NormalizedHostName.ToLowerInvariant(), "-");
        return $"robots-{host}.txt";
    }

    [GeneratedRegex("[^a-zA-Z0-9._-]+")]
    private static partial Regex InvalidFileNameCharacters();
}
