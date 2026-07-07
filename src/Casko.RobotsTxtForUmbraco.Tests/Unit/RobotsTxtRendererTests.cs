using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Casko.RobotsTxtForUmbraco.Models;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtRendererTests
{
    [Test]
    public void Render_WritesGroupsAndSitemaps()
    {
        var document = new RobotsTxtDocument
        {
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = ["*"],
                    Allow = ["/public"],
                    Disallow = ["/private", "/private"]
                }
            ],
            Sitemaps = ["https://example.com/sitemap.xml"]
        };

        var result = new RobotsTxtRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: *" + Environment.NewLine +
            "Allow: /public" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine +
            Environment.NewLine +
            "Sitemap: https://example.com/sitemap.xml" + Environment.NewLine));
    }
}
