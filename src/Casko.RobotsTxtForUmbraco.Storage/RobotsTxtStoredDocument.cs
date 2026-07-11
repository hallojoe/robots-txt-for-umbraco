namespace Casko.RobotsTxtForUmbraco.Storage;

public sealed record RobotsTxtStoredDocument(
    RobotsTxtStorageKey Key,
    Guid MediaKey,
    int MediaId,
    string FileName,
    string? MediaPath,
    string Text,
    DateTimeOffset? RefreshedUtc);
