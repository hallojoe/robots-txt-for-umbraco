using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class DefaultRobotsTxtServiceTests
{
    [Test]
    public async Task GetAsync_WhenHostMatches_ResolvesProfilesAndHostOverrides()
    {
        var options = new RobotsTxtOptions
        {
            DefaultHost = "default",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
                {
                    Profiles = ["default-sitemap"]
                },
                ["host-1"] = new()
                {
                    HostName = "example.com",
                    FrontendHostName = "https://www.example.com",
                    Profiles = ["disallow-base", "allow-public"],
                    Sitemaps = ["/host-sitemap.xml"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/admin"] }
                    }
                }
            },
            Profiles = new Dictionary<string, RobotsTxtProfileOptions>
            {
                ["default-sitemap"] = new()
                {
                    Sitemaps = ["/default.xml"]
                },
                ["disallow-base"] = new()
                {
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/private"] }
                    }
                },
                ["allow-public"] = new()
                {
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Allow = ["/public"] }
                    }
                },
                ["block-ai"] = new()
                {
                    DisallowUserAgents = ["GPTBot", "ChatGPT-user"]
                }
            }
        };

        options.Hosts["host-1"].Profiles.Add("block-ai");

        var result = await CreateService(options).GetAsync("example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.SitemapBaseUrl, Is.EqualTo("https://www.example.com"));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "/host-sitemap.xml" }));
            Assert.That(result.DisallowUserAgents, Is.EqualTo(new[] { "GPTBot", "ChatGPT-user" }));
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "*" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private", "/admin" }));
        });
    }

    [Test]
    public async Task GetAsync_WhenNoHostMatches_FallsBackToDefaultHost()
    {
        var options = new RobotsTxtOptions
        {
            DefaultHost = "default",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
                {
                    Profiles = ["disallow-all"]
                }
            },
            Profiles = new Dictionary<string, RobotsTxtProfileOptions>
            {
                ["disallow-all"] = new()
                {
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/"] }
                    }
                }
            }
        };

        var result = await CreateService(options).GetAsync("unknown.example");

        Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/" }));
    }

    [Test]
    public async Task GetAsync_WhenProfilesShareUserAgent_MergesListsCaseInsensitively()
    {
        var options = new RobotsTxtOptions
        {
            DefaultHost = "default",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
                {
                    Profiles = ["profile-1", "profile-2"]
                }
            },
            Profiles = new Dictionary<string, RobotsTxtProfileOptions>
            {
                ["profile-1"] = new()
                {
                    DisallowUserAgents = ["GPTBot"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["GoogleBot"] = new() { Disallow = ["/private"] }
                    }
                },
                ["profile-2"] = new()
                {
                    DisallowUserAgents = ["gptbot", "ChatGPT-user"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["googlebot"] = new() { Allow = ["/public"] }
                    }
                }
            }
        };

        var result = await CreateService(options).GetAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(result.DisallowUserAgents, Is.EqualTo(new[] { "GPTBot", "ChatGPT-user" }));
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "GoogleBot" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }

    private static DefaultRobotsTxtService CreateService(RobotsTxtOptions options)
    {
        return new DefaultRobotsTxtService(Options.Create(options));
    }
}
