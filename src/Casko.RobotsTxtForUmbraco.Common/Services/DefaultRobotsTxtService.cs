using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Models;
using Microsoft.Extensions.Options;
namespace Casko.RobotsTxtForUmbraco.Common.Services;

public sealed class DefaultRobotsTxtService(
    IOptions<RobotsTxtOptions> options) : IRobotsTxtService
{
    /// <inheritdoc />
    public async Task<RobotsTxtDocument> GetAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = options.Value;
        var configuredFile = RobotsTxtOptionsResolver.Resolve(settings, hostName);
        var document = CreateConfiguredDocument(configuredFile);
  
        return document;
    }

    private static RobotsTxtDocument CreateConfiguredDocument(RobotsTxtHostOptions? configuredFile)
    {
        if (configuredFile is null)
        {
            return new RobotsTxtDocument();
        }

        return new RobotsTxtDocument
        {
            DisallowUserAgents = Clean(configuredFile.DisallowUserAgents).ToList(),
            SitemapBaseUrl = ResolveSitemapBaseUrl(configuredFile),
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

    private static string? ResolveSitemapBaseUrl(RobotsTxtHostOptions configuredFile)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredFile.FrontendHostName)
            ? configuredFile.HostName
            : configuredFile.FrontendHostName;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
        if (!normalizedBaseUrl.Contains("://", StringComparison.Ordinal))
        {
            normalizedBaseUrl = "https://" + normalizedBaseUrl;
        }

        return Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var uri)
            ? uri.ToString().TrimEnd('/')
            : null;
    }

    private static IEnumerable<string> Clean(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
