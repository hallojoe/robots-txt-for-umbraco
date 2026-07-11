namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public sealed class RobotsTxtOptions
{
    public const string Key = "RobotsTxt";

    public bool Enabled { get; init; } = true;

    public bool RewritesEnabled { get; init; } = true;

    public bool UseDeliveryApiAccessPolicy { get; init; }

    /// <summary>
    /// Gets or sets the runtime host bindings. Use an empty `Host` key for the default binding.
    /// </summary>
    public Dictionary<string, RobotsTxtBindingOptions> Hosts { get; set; } = [];

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
/// Runtime bindings-specific robots.txt configuration.
/// </summary>
public sealed class RobotsTxtBindingOptions
{
    /// <summary>
    /// Gets or sets the hostname used when resolving sitemap URLs.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the frontend URL used when resolving sitemap URLs.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// List of sitemap URLs emitted by this binding.
    /// </summary>
    public string[] Sitemaps { get; set; } = [];
    
    /// <summary>
    /// Ordered partial robots.txt files to include for this binding.
    /// </summary>
    public string[] Include { get; set; } = [];
}
