namespace Casko.RobotsTxtForUmbraco.Storage;

public sealed record RobotsTxtStorageKey(string? HostName)
{
    public string NormalizedHostName => string.IsNullOrWhiteSpace(HostName)
        ? "default"
        : HostName.Trim().TrimEnd('/').Replace(":", "-");
}
