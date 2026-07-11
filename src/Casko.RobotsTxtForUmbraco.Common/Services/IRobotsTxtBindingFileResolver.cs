namespace Casko.RobotsTxtForUmbraco.Common.Services;

public interface IRobotsTxtBindingFileResolver
{
    string ResolvePath(string includePath);

    bool Exists(string includePath);

    Task<string> ReadAsync(string includePath, CancellationToken cancellationToken = default);
}
