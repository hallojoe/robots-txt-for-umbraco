using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtOptionsTests
{
    [Test]
    public void Bind_BindsRootNodeSearchLevelAndFiles()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RobotsTxtOptions.Key}:RootNodeSearchLevel"] = "1",
                [$"{RobotsTxtOptions.Key}:HostingDocumentTypeAliases:0"] = "home",
                [$"{RobotsTxtOptions.Key}:Files:default:DisallowScanEnabled"] = "true",
                [$"{RobotsTxtOptions.Key}:Files:default:Sitemaps:0"] = "https://example.com/sitemap.xml",
                [$"{RobotsTxtOptions.Key}:Files:default:UserAgents:*:Disallow:0"] = "/private"
            })
            .Build();

        var options = new RobotsTxtOptions();
        configuration.GetSection(RobotsTxtOptions.Key).Bind(options);

        Assert.Multiple(() =>
        {
            Assert.That(options.RootNodeSearchLevel, Is.EqualTo(1));
            Assert.That(options.HostingDocumentTypeAliases, Is.EqualTo(new[] { "home" }));
            Assert.That(options.Files.ContainsKey("default"), Is.True);
            Assert.That(options.Files["default"].DisallowScanEnabled, Is.True);
            Assert.That(options.Files["default"].Sitemaps, Is.EqualTo(new[] { "https://example.com/sitemap.xml" }));
            Assert.That(options.Files["default"].UserAgents["*"].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }
}
