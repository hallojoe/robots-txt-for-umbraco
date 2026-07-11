using Casko.RobotsTxtForUmbraco.Common.Services;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public static class RobotsTxtOptionsResolver
{
    public static RobotsTxtHostOptions? Resolve(RobotsTxtOptions options, string? hostName)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Hosts.Count > 0)
        {
            var exactHostMatch = options.Hosts.Values.FirstOrDefault(host => RobotsTxtHostName.IsMatch(host.HostName, hostName));
            if (exactHostMatch is not null)
            {
                return MergeHostWithProfiles(exactHostMatch, options.Profiles);
            }

            if (options.Hosts.TryGetValue(options.DefaultHost, out var defaultHost))
            {
                return MergeHostWithProfiles(defaultHost, options.Profiles);
            }

            return null;
        }

        if (options.Files.Count == 0)
        {
            return null;
        }

        var legacyFile = ResolveLegacyFile(options.Files, hostName);
        return legacyFile is null ? null : MapLegacyFile(legacyFile);
    }

    public static string?[] GetConfiguredHostNames(RobotsTxtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IEnumerable<string?> configuredHosts = options.Hosts.Count > 0
            ? options.Hosts.Values.Select(host => host.HostName)
            : options.Files.Values.Select(file => file.HostName);

        return configuredHosts
            .Append(null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static RobotsTxtFileOptions? ResolveLegacyFile(
        IReadOnlyDictionary<string, RobotsTxtFileOptions> files,
        string? hostName)
    {
        var exactHostMatch = files.Values.FirstOrDefault(file => RobotsTxtHostName.IsMatch(file.HostName, hostName));
        if (exactHostMatch is not null)
        {
            return exactHostMatch;
        }

        var hostlessMatch = files.Values.FirstOrDefault(file => string.IsNullOrWhiteSpace(file.HostName));
        if (hostlessMatch is not null)
        {
            return hostlessMatch;
        }

        if (files.TryGetValue(string.Empty, out var defaultFile))
        {
            return defaultFile;
        }

        if (files.TryGetValue("*", out defaultFile))
        {
            return defaultFile;
        }

        if (files.TryGetValue("default", out defaultFile))
        {
            return defaultFile;
        }

        return files.Values.FirstOrDefault();
    }

    private static RobotsTxtHostOptions MergeHostWithProfiles(
        RobotsTxtHostOptions host,
        IReadOnlyDictionary<string, RobotsTxtProfileOptions> profiles)
    {
        var mergedHost = new RobotsTxtHostOptions
        {
            HostName = Clean(host.HostName),
            FrontendHostName = Clean(host.FrontendHostName),
            DisallowUserAgents = [],
            UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>(StringComparer.OrdinalIgnoreCase),
            Profiles = host.Profiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile))
                .Select(profile => profile.Trim())
                .ToList()
        };

        foreach (var profileKey in mergedHost.Profiles)
        {
            if (profiles.TryGetValue(profileKey, out var profile))
            {
                MergeProfileContent(mergedHost, profile);
            }
        }

        MergeProfileContent(mergedHost, host);
        return mergedHost;
    }

    private static void MergeProfileContent(RobotsTxtProfileOptions target, RobotsTxtProfileOptions source)
    {
        AddDistinct(target.DisallowUserAgents, source.DisallowUserAgents);

        foreach (var sitemap in Clean(source.Sitemaps))
        {
            if (!target.Sitemaps.Contains(sitemap, StringComparer.OrdinalIgnoreCase))
            {
                target.Sitemaps.Add(sitemap);
            }
        }

        foreach (var entry in source.UserAgents)
        {
            var userAgentKey = Clean(entry.Key);
            if (userAgentKey is null)
            {
                continue;
            }

            var existingKey = target.UserAgents.Keys.FirstOrDefault(key => string.Equals(key, userAgentKey, StringComparison.OrdinalIgnoreCase));
            if (existingKey is null)
            {
                existingKey = userAgentKey;
                target.UserAgents[existingKey] = new RobotsTxtUserAgentOptions
                {
                    Allow = [],
                    Disallow = []
                };
            }

            var existing = target.UserAgents[existingKey];
            AddDistinct(existing.Allow, entry.Value.Allow);
            AddDistinct(existing.Disallow, entry.Value.Disallow);
        }
    }

    private static RobotsTxtHostOptions MapLegacyFile(RobotsTxtFileOptions file)
    {
        var host = new RobotsTxtHostOptions
        {
            HostName = Clean(file.HostName),
            FrontendHostName = Clean(file.FrontendHostName),
            DisallowUserAgents = [],
            UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>(StringComparer.OrdinalIgnoreCase)
        };

        MergeProfileContent(host, new RobotsTxtProfileOptions
        {
            Sitemaps = file.Sitemaps,
            UserAgents = file.UserAgents
        });

        return host;
    }

    private static void AddDistinct(List<string> target, IEnumerable<string> values)
    {
        foreach (var value in Clean(values))
        {
            if (!target.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                target.Add(value);
            }
        }
    }

    private static IEnumerable<string> Clean(IEnumerable<string> values)
    {
        return values
            .Select(Clean)
            .Where(value => value is not null)!
            .Cast<string>();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
