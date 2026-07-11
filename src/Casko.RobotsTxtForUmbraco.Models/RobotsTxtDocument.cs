namespace Casko.RobotsTxtForUmbraco.Models;

public sealed class RobotsTxtDocument
{
    public List<string> DisallowUserAgents { get; set; } = [];

    public List<RobotsTxtGroup> Groups { get; set; } = [];

    public string? SitemapBaseUrl { get; set; }

    public List<string> Sitemaps { get; set; } = [];
}
