using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Casko.RobotsTxtForUmbraco.Models;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Common.Services;

public sealed class DefaultRobotsTxtService(
    IOptions<RobotsTxtOptions> options,
    IRobotsTxtRenderer robotsTxtRenderer,
    IRobotsTxtBindingFileResolver bindingFileResolver) : IRobotsTxtService
{
    /// <inheritdoc />
    public async Task<RobotsTxtDocument> GetAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedBinding = RobotsTxtOptionsResolver.Resolve(options.Value, hostName);
        if (resolvedBinding is null)
        {
            return new RobotsTxtDocument();
        }

        var document = new RobotsTxtDocument();

        foreach (var include in Clean(resolvedBinding.Binding.Include))
        {
            var fileContents = await bindingFileResolver.ReadAsync(include, cancellationToken);
            var partialDocument = robotsTxtRenderer.Parse(fileContents);
            document = robotsTxtRenderer.Merge(document, partialDocument);
        }

        document.SitemapBaseUrl = ResolveSitemapBaseUrl(resolvedBinding);
        document.Sitemaps = Clean(document.Sitemaps.Concat(resolvedBinding.Binding.Sitemaps)).ToList();

        return document;
    }

    private static string? ResolveSitemapBaseUrl(RobotsTxtResolvedBinding resolvedBinding)
    {
        var baseUrl = string.IsNullOrWhiteSpace(resolvedBinding.Binding.Url)
            ? resolvedBinding.Binding.Host
            : resolvedBinding.Binding.Url;

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
