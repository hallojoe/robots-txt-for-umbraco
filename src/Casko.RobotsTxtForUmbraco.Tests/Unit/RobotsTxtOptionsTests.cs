using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtOptionsTests
{
    [Test]
    public void Bind_BindsHostsProfilesAndDefaultHost()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RobotsTxtOptions.Key}:DefaultHost"] = "default",
                [$"{RobotsTxtOptions.Key}:Hosts:default:Profiles:0"] = "disallow-all",
                [$"{RobotsTxtOptions.Key}:Profiles:disallow-all:DisallowUserAgents:0"] = "GPTBot",
                [$"{RobotsTxtOptions.Key}:Profiles:disallow-all:UserAgents:*:Disallow:0"] = "/private",
                [$"{RobotsTxtOptions.Key}:Profiles:default-sitemap:Sitemaps:0"] = "/sitemap.xml"
            })
            .Build();

        var options = new RobotsTxtOptions();
        configuration.GetSection(RobotsTxtOptions.Key).Bind(options);

        Assert.Multiple(() =>
        {
            Assert.That(options.DefaultHost, Is.EqualTo("default"));
            Assert.That(options.Hosts.ContainsKey("default"), Is.True);
            Assert.That(options.Hosts["default"].Profiles, Is.EqualTo(new[] { "disallow-all" }));
            Assert.That(options.Profiles["disallow-all"].DisallowUserAgents, Is.EqualTo(new[] { "GPTBot" }));
            Assert.That(options.Profiles["disallow-all"].UserAgents["*"].Disallow, Is.EqualTo(new[] { "/private" }));
            Assert.That(options.Profiles["default-sitemap"].Sitemaps, Is.EqualTo(new[] { "/sitemap.xml" }));
        });
    }

    [Test]
    public void Resolve_WhenUsingLegacyFiles_FallsBackToLegacyModel()
    {
        var options = new RobotsTxtOptions
        {
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/private"] }
                    }
                }
            }
        };

        var resolved = RobotsTxtOptionsResolver.Resolve(options, "unknown.example");

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.UserAgents["*"].Disallow, Is.EqualTo(new[] { "/private" }));
    }

    [Test]
    public void ResolveHostNames_WhenUsingLegacyFiles_ReturnsConfiguredHostsAndDefault()
    {
        var result = RobotsTxtOptionsResolver.GetConfiguredHostNames(new RobotsTxtOptions
        {
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new() { HostName = null },
                ["host-1"] = new() { HostName = "example.com" }
            }
        });

        Assert.That(result, Is.EquivalentTo(new string?[] { null, "example.com" }));
    }
}
