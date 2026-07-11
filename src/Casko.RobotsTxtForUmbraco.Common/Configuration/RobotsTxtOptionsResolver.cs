using Casko.RobotsTxtForUmbraco.Common.Services;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public static class RobotsTxtOptionsResolver
{
    public static RobotsTxtResolvedBinding? Resolve(RobotsTxtOptions options, string? hostName)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var entry in options.Hosts)
        {
            if (string.IsNullOrWhiteSpace(entry.Value.Host))
            {
                continue;
            }

            if (RobotsTxtHostName.IsMatch(entry.Value.Host, hostName))
            {
                return new RobotsTxtResolvedBinding(Clean(entry.Key), entry.Value);
            }
        }

        return options.Hosts.TryGetValue(string.Empty, out var defaultBinding)
            ? new RobotsTxtResolvedBinding(null, defaultBinding)
            : null;
    }

    public static string?[] GetConfiguredHostNames(RobotsTxtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Hosts.Keys
            .Select(binding => options.Hosts[binding].Host)
            .Where(hostName => !string.IsNullOrWhiteSpace(hostName))
            .Cast<string?>()
            .Append(null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record RobotsTxtResolvedBinding(
    string? HostName,
    RobotsTxtBindingOptions Binding);
