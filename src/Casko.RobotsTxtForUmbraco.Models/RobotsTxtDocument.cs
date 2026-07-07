namespace Casko.RobotsTxtForUmbraco.Models;

public sealed class RobotsTxtDocument
{
    public List<RobotsTxtGroup> Groups { get; set; } = [];

    public List<string> Sitemaps { get; set; } = [];
}
