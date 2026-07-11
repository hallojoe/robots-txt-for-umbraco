namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public sealed class RobotsTxtOptions
{
    public const string Key = "RobotsTxt";

    public bool Enabled { get; set; } = true;

    public bool RewritesEnabled { get; set; } = true;

    public bool UseDeliveryApiAccessPolicy { get; set; }

    /// <summary>
    /// Gets or sets the runtime host configurations keyed by internal host identifier.
    /// </summary>
    public Dictionary<string, RobotsTxtHostOptions> Hosts { get; set; } = [];

    /// <summary>
    /// Gets or sets reusable robots.txt content profiles keyed by profile identifier.
    /// </summary>
    public Dictionary<string, RobotsTxtProfileOptions> Profiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the fallback host key used when no host-specific entry matches.
    /// </summary>
    public string DefaultHost { get; set; } = "*";

    /// <summary>
    /// Gets or sets the legacy per-file configuration model. Prefer <see cref="Hosts"/> and <see cref="Profiles"/>.
    /// </summary>
    public Dictionary<string, RobotsTxtFileOptions> Files { get; set; } = [];

    public RobotsTxtStorageOptions Storage { get; set; } = new();
}

public sealed class RobotsTxtStorageOptions
{
    /// <summary>
    /// Gets or sets the number of seconds after which a stored robots.txt document is considered stale.
    /// </summary>
    public int RefreshStaleAfterSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the options for the background refresh job.
    /// </summary>
    public RobotsTxtStorageBackgroundJobOptions BackgroundJob { get; set; } = new();
}

public sealed class RobotsTxtStorageBackgroundJobOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the background refresh job is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in seconds between background refresh jobs.
    /// </summary>
    public int IntervalSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the number of seconds to delay the background refresh job.
    /// </summary>
    public int RefreshJobDelayInSeconds { get; set; } = 10;
}

/// <summary>
/// Reusable robots.txt profile content.
/// </summary>
public class RobotsTxtProfileOptions
{
    /// <summary>
    /// A list of user agents to disallow by default.
    /// </summary>
    public List<string> DisallowUserAgents { get; set; } = []; 

    /// <summary>
    /// Gets or sets sitemap URLs emitted by this configuration.
    /// </summary>
    public List<string> Sitemaps { get; set; } = [];

    /// <summary>
    /// Gets or sets user-agent rules keyed by user-agent value.
    /// </summary>
    public Dictionary<string, RobotsTxtUserAgentOptions> UserAgents { get; set; } = [];
}

/// <summary>
/// Runtime host-specific robots.txt configuration.
/// </summary>
public sealed class RobotsTxtHostOptions : RobotsTxtProfileOptions
{
    /// <summary>
    /// Gets or sets the host name this file applies to.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the frontend URL. Only necessary when frontend-URL is different from URL found in the hostname.
    /// </summary>
    public string? FrontendHostName { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of reusable profiles to apply to this host.
    /// </summary>
    public List<string> Profiles { get; set; } = [];
}

/// <summary>
/// Individual robots.txt file configuration used by the legacy <see cref="RobotsTxtOptions.Files"/> model.
/// </summary>
public sealed class RobotsTxtFileOptions
{
    /// <summary>
    /// Gets or sets the host name this file applies to.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the frontend URL. Only necessary when frontend-URL is different from URL found in the hostname.
    /// </summary>
    public string? FrontendHostName { get; set; }

    /// <summary>
    /// Gets or sets sitemap URLs emitted by this file.
    /// </summary>
    public List<string> Sitemaps { get; set; } = [];

    /// <summary>
    /// Gets or sets user-agent rules keyed by user-agent value.
    /// </summary>
    public Dictionary<string, RobotsTxtUserAgentOptions> UserAgents { get; set; } = [];
}

/// <summary>
/// User-agent configuration for a robots.txt file.
/// </summary>
public sealed class RobotsTxtUserAgentOptions
{
    public List<string> Allow { get; set; } = [];

    public List<string> Disallow { get; set; } = [];
}
