using System.Text;
using Casko.RobotsTxtForUmbraco.Models;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Rendering;

public sealed class RobotsTxtRenderer : IRobotsTxtRenderer
{
    /// <inheritdoc />
    public string Render(RobotsTxtDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        var wroteBlock = false;
        var sitemapBaseUri = TryCreateAbsoluteUri(document.SitemapBaseUrl);
        var disallowedUserAgents = DistinctClean(document.DisallowUserAgents).ToArray();
        var disallowedUserAgentSet = disallowedUserAgents.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (disallowedUserAgents.Length > 0)
        {
            foreach (var userAgent in disallowedUserAgents)
            {
                builder.Append("User-agent: ").AppendLine(userAgent);
            }

            builder.AppendLine("Disallow: /");
            wroteBlock = true;
        }

        foreach (var group in document.Groups.Where(group => group.UserAgents.Count > 0))
        {
            var userAgents = DistinctUserAgents(group.UserAgents)
                .Where(userAgent => !disallowedUserAgentSet.Contains(userAgent))
                .ToArray();

            if (userAgents.Length == 0)
            {
                continue;
            }

            if (wroteBlock)
            {
                builder.AppendLine();
            }

            foreach (var userAgent in userAgents)
            {
                builder.Append("User-agent: ").AppendLine(userAgent);
            }

            foreach (var disallow in DistinctClean(group.Disallow))
            {
                builder.Append("Disallow: ").AppendLine(disallow);
            }
            
            foreach (var allow in DistinctClean(group.Allow))
            {
                builder.Append("Allow: ").AppendLine(allow);
            }

            wroteBlock = true;
        }

        var sitemaps = DistinctClean(document.Sitemaps)
            .Select(sitemap => ResolveSitemapUrl(sitemap, sitemapBaseUri))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sitemaps.Length > 0)
        {
            if (wroteBlock)
            {
                builder.AppendLine();
            }

            foreach (var sitemap in sitemaps)
            {
                builder.Append("Sitemap: ").AppendLine(sitemap);
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public RobotsTxtDocument Parse(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new RobotsTxtDocument();
        RobotsTxtGroup? currentGroup = null;
        var currentGroupHasRules = false;

        using var reader = new StringReader(document);

        while (reader.ReadLine() is { } rawLine)
        {
            var line = CleanLine(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!TryParseDirective(line, out var directive, out var value))
            {
                continue;
            }

            switch (directive)
            {
                case "user-agent":
                    if (currentGroup is null || currentGroupHasRules)
                    {
                        currentGroup = new RobotsTxtGroup();
                        result.Groups.Add(currentGroup);
                        currentGroupHasRules = false;
                    }

                    currentGroup.UserAgents.Add(value);
                    break;

                case "disallow":
                    if (currentGroup is null || currentGroup.UserAgents.Count == 0)
                    {
                        continue;
                    }

                    currentGroup.Disallow.Add(value);
                    currentGroupHasRules = true;
                    break;
                
                case "allow":
                    if (currentGroup is null || currentGroup.UserAgents.Count == 0)
                    {
                        continue;
                    }

                    currentGroup.Allow.Add(value);
                    currentGroupHasRules = true;
                    break;

                case "sitemap":
                    result.Sitemaps.Add(value);
                    break;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public RobotsTxtDocument Merge(RobotsTxtDocument document1, RobotsTxtDocument document2)
    {
        ArgumentNullException.ThrowIfNull(document1);
        ArgumentNullException.ThrowIfNull(document2);

        var groups = document1.Groups
            .Concat(document2.Groups)
            .Select(group => new
            {
                UserAgents = NormalizeUserAgents(group.UserAgents).ToArray(),
                Allow = DistinctClean(group.Allow).ToArray(),
                Disallow = DistinctClean(group.Disallow).ToArray()
            })
            .Where(group => group.UserAgents.Length > 0)
            .GroupBy(
                group => string.Join("\n", group.UserAgents.OrderBy(userAgent => userAgent, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new RobotsTxtGroup
            {
                UserAgents = group
                    .SelectMany(entry => entry.UserAgents)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(userAgent => userAgent, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Allow = group
                    .SelectMany(entry => entry.Allow)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Disallow = group
                    .SelectMany(entry => entry.Disallow)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .ToList();

        return new RobotsTxtDocument
        {
            DisallowUserAgents = DistinctClean(document1.DisallowUserAgents.Concat(document2.DisallowUserAgents)).ToList(),
            SitemapBaseUrl = FirstNonEmpty(document1.SitemapBaseUrl, document2.SitemapBaseUrl),
            Groups = groups,
            Sitemaps = DistinctClean(document1.Sitemaps.Concat(document2.Sitemaps)).ToList()
        };
    }

    private static IEnumerable<string> DistinctClean(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> DistinctUserAgents(IEnumerable<string> values)
    {
        return NormalizeUserAgents(values);
    }

    private static IEnumerable<string> NormalizeUserAgents(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(string? value1, string? value2)
    {
        if (!string.IsNullOrWhiteSpace(value1))
        {
            return value1.Trim();
        }

        return string.IsNullOrWhiteSpace(value2) ? null : value2.Trim();
    }

    private static string CleanLine(string line)
    {
        var commentIndex = line.IndexOf('#');
        var value = commentIndex >= 0 ? line[..commentIndex] : line;
        return value.Trim();
    }

    private static bool TryParseDirective(string line, out string directive, out string value)
    {
        directive = string.Empty;
        value = string.Empty;

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return false;
        }

        directive = line[..separatorIndex].Trim().ToLowerInvariant();
        value = line[(separatorIndex + 1)..].Trim();
        return value.Length > 0;
    }

    private static string ResolveSitemapUrl(string sitemap, Uri? sitemapBaseUri)
    {
        if (Uri.TryCreate(sitemap, UriKind.Absolute, out _))
        {
            return sitemap;
        }

        if (sitemapBaseUri is null)
        {
            return sitemap;
        }

        return new Uri(sitemapBaseUri, sitemap).ToString();
    }

    private static Uri? TryCreateAbsoluteUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}
