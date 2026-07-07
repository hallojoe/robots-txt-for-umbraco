using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Cms;
using Casko.RobotsTxtForUmbraco.Models;
using Microsoft.Extensions.Options;
namespace Casko.RobotsTxtForUmbraco.Common.Services;

public sealed class DefaultRobotsTxtService(
    IOptions<RobotsTxtOptions> options,
    IRobotsTxtCmsContentService cmsContentService) : IRobotsTxtService
{
    /// <inheritdoc />
    public async Task<RobotsTxtDocument> GetAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var configuredFile = ResolveConfiguredFile(settings, hostName);
        var document = CreateConfiguredDocument(configuredFile);
        if (!settings.Enabled || configuredFile is null)
        {
            return document;
        }

        if (!configuredFile.DisallowScanEnabled)
        {
            return document;
        }

        var generatedDisallows = CollectGeneratedDisallows(settings, configuredFile.HostName ?? hostName);
        MergeGeneratedDisallows(document, generatedDisallows);

        return document;
    }

    private IEnumerable<string> CollectGeneratedDisallows(RobotsTxtOptions settings, string? hostName)
    {
        if (string.IsNullOrWhiteSpace(settings.ExcludingUrlPropertyAlias))
        {
            return [];
        }

        return cmsContentService.GetDisallowedContents(hostName);
    }

    private static RobotsTxtDocument CreateConfiguredDocument(RobotsTxtFileOptions? configuredFile)
    {
        if (configuredFile is null)
        {
            return new RobotsTxtDocument();
        }

        return new RobotsTxtDocument
        {
            Groups = configuredFile.UserAgents
                .Select(entry => new RobotsTxtGroup
                {
                    UserAgents = Clean([entry.Key]).ToList(),
                    Allow = Clean(entry.Value.Allow).ToList(),
                    Disallow = Clean(entry.Value.Disallow).ToList()
                })
                .Where(group => group.UserAgents.Count > 0)
                .ToList(),
            Sitemaps = Clean(configuredFile.Sitemaps).ToList()
        };
    }

    private static void MergeGeneratedDisallows(RobotsTxtDocument document, IEnumerable<string> generatedDisallows)
    {
        var paths = generatedDisallows.ToArray();
        if (paths.Length == 0)
        {
            return;
        }

        var group = document.Groups.FirstOrDefault(group =>
            group.UserAgents.Any(userAgent => string.Equals(userAgent, "*", StringComparison.OrdinalIgnoreCase)));

        if (group is null)
        {
            group = new RobotsTxtGroup { UserAgents = ["*"] };
            document.Groups.Add(group);
        }

        group.Disallow = group.Disallow
            .Concat(paths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static RobotsTxtFileOptions? ResolveConfiguredFile(RobotsTxtOptions settings, string? hostName)
    {
        if (settings.Files.Count == 0)
        {
            return null;
        }

        var exactHostMatch = settings.Files.Values.FirstOrDefault(file => RobotsTxtHostName.IsMatch(file.HostName, hostName));
        if (exactHostMatch is not null)
        {
            return exactHostMatch;
        }

        var hostlessMatch = settings.Files.Values.FirstOrDefault(file => string.IsNullOrWhiteSpace(file.HostName));
        if (hostlessMatch is not null)
        {
            return hostlessMatch;
        }

        if (settings.Files.TryGetValue("default", out var defaultFile))
        {
            return defaultFile;
        }

        return settings.Files.Values.FirstOrDefault();
    }

    private static IEnumerable<string> Clean(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
