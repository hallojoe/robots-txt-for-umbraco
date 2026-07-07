namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public sealed class RobotsTxtOptions
{
    public const string Key = "RobotsTxt";

    public bool Enabled { get; set; } = true;

    public bool RewritesEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the level where routed root nodes are resolved.
    /// </summary>
    public int RootNodeSearchLevel { get; set; }

    public List<string> IncludedContentTypeAliases { get; set; } = [];

    public List<string> ExcludedContentTypeAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the property alias of the property that determines whether a content item is excluded from robots.txt.
    /// When <see cref="ExcludingUrlPropertyValue"/> is set to "1", the content item path is written to robots.txt as "Disallow:".
    /// </summary>
    public string? ExcludingUrlPropertyAlias { get; set; } = "umbracoNaviHide";

    /// <summary>
    /// Gets or sets the match value of the property that determines whether the content item path is written to robots.txt as "Disallow:".
    /// </summary>
    public string? ExcludingUrlPropertyValue { get; set; } = "1";

    public string[] RootDocumentTypeAliases { get; set; } = [];

    public string[] HostingDocumentTypeAliases { get; set; } = [];

    public List<string> IncludedCultures { get; set; } = [];

    public List<string> ExcludedCultures { get; set; } = [];

    public bool UseDeliveryApiAccessPolicy { get; set; }

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
/// Individual robots.txt file configuration.
/// </summary>
public sealed class RobotsTxtFileOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether property-based disallow scanning is enabled.
    /// </summary>
    public bool DisallowScanEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the host name this file applies to.
    /// </summary>
    public string? HostName { get; set; }

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
