namespace Casko.RobotsTxtForUmbraco.Models;

public sealed class RobotsTxtGroup
{
    public List<string> UserAgents { get; set; } = [];

    public List<string> Allow { get; set; } = [];

    public List<string> Disallow { get; set; } = [];
}
