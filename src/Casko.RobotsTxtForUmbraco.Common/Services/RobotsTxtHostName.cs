namespace Casko.RobotsTxtForUmbraco.Common.Services;

public static class RobotsTxtHostName
{
    public static string? Normalize(string? hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return null;
        }

        var value = hostName.Trim().TrimEnd('/');
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = "https://" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return hostName.Trim().TrimEnd('/');
        }

        return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    }

    public static bool IsMatch(string? configuredHostName, string? requestedHostName)
    {
        var normalizedConfiguredHost = Normalize(configuredHostName);
        var normalizedRequestedHost = Normalize(requestedHostName);

        if (string.IsNullOrWhiteSpace(normalizedConfiguredHost) ||
            string.IsNullOrWhiteSpace(normalizedRequestedHost))
        {
            return false;
        }

        return string.Equals(normalizedConfiguredHost, normalizedRequestedHost, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMatchAgainstAbsoluteUrl(string? requestedHostName, string? absoluteContentUrl)
    {
        if (string.IsNullOrWhiteSpace(requestedHostName) ||
            string.IsNullOrWhiteSpace(absoluteContentUrl) ||
            absoluteContentUrl.EndsWith('#'))
        {
            return false;
        }

        if (!Uri.TryCreate(absoluteContentUrl, UriKind.Absolute, out var contentUri))
        {
            return false;
        }

        return IsMatch(requestedHostName, contentUri.Authority);
    }
}
