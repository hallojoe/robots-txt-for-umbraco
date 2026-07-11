using Microsoft.Extensions.Hosting;

namespace Casko.RobotsTxtForUmbraco.Common.Services;

public sealed class ContentRootRobotsTxtBindingFileResolver(
    IHostEnvironment hostEnvironment) : IRobotsTxtBindingFileResolver
{
    private readonly string _contentRootPath = NormalizeDirectory(hostEnvironment.ContentRootPath);

    public string ResolvePath(string includePath)
    {
        if (string.IsNullOrWhiteSpace(includePath))
        {
            throw new InvalidOperationException("RobotsTxt binding include entries cannot be empty.");
        }

        var candidatePath = includePath.Trim();
        var fullPath = Path.GetFullPath(
            Path.IsPathRooted(candidatePath)
                ? candidatePath
                : Path.Combine(_contentRootPath, candidatePath));

        if (!fullPath.StartsWith(_contentRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"RobotsTxt binding include '{candidatePath}' must resolve inside the content root '{_contentRootPath}'.");
        }

        return fullPath;
    }

    public bool Exists(string includePath)
    {
        try
        {
            return File.Exists(ResolvePath(includePath));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task<string> ReadAsync(string includePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(includePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"RobotsTxt binding include '{includePath}' was not found in content root '{_contentRootPath}'.",
                fullPath);
        }

        return File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    private static string NormalizeDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
