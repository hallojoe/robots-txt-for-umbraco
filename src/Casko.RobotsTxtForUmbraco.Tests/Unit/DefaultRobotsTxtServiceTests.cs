using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Cms;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class DefaultRobotsTxtServiceTests
{
    private IRobotsTxtCmsContentService _cmsContentService = null!;

    [SetUp]
    public void SetUp()
    {
        _cmsContentService = Substitute.For<IRobotsTxtCmsContentService>();
    }

    [Test]
    public async Task GetAsync_WhenHostMatchesConfiguredFile_UsesMatchedFile()
    {
        var sut = CreateService(new RobotsTxtOptions
        {
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    HostName = null,
                    Sitemaps = ["https://example.com/default.xml"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/default"] }
                    }
                },
                ["host-1"] = new()
                {
                    HostName = "site.example.com",
                    Sitemaps = ["https://site.example.com/sitemap.xml"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["bingbot"] = new() { Allow = ["/public"] }
                    },
                    DisallowScanEnabled = false
                }
            }
        });

        var result = await sut.GetAsync("https://site.example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "https://site.example.com/sitemap.xml" }));
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "bingbot" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
        });
    }

    [Test]
    public async Task GetAsync_WhenHostDoesNotMatch_UsesHostlessFileBeforeDefaultKeyFallback()
    {
        var sut = CreateService(new RobotsTxtOptions
        {
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    HostName = "default.example.com",
                    Sitemaps = ["https://default.example.com/sitemap.xml"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/default"] }
                    },
                    DisallowScanEnabled = false
                },
                ["fallback"] = new()
                {
                    HostName = null,
                    Sitemaps = ["https://fallback.example.com/sitemap.xml"],
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/fallback"] }
                    },
                    DisallowScanEnabled = false
                }
            }
        });

        var result = await sut.GetAsync("unknown.example.com");

        Assert.That(result.Sitemaps, Is.EqualTo(new[] { "https://fallback.example.com/sitemap.xml" }));
    }

    [Test]
    public async Task GetAsync_WhenDisallowScanIsDisabled_DoesNotScanContent()
    {
        var sut = CreateService(new RobotsTxtOptions
        {
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    DisallowScanEnabled = false,
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/configured"] }
                    }
                }
            }
        });

        var result = await sut.GetAsync("example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Single().Disallow, Is.EqualTo(new[] { "/configured" }));
            _cmsContentService.DidNotReceiveWithAnyArgs().GetDisallowedContents(default);
        });
    }

    [Test]
    public async Task GetAsync_WhenDisallowScanIsEnabled_MergesGeneratedDisallowsIntoWildcardGroup()
    {
        _cmsContentService.GetDisallowedContents("example.com").Returns(new[] { "/hidden" });

        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1",
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    HostName = "example.com",
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["*"] = new() { Disallow = ["/configured"] }
                    }
                }
            }
        });

        var result = await sut.GetAsync("example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Single().Disallow, Is.EqualTo(new[] { "/configured", "/hidden" }));
            _cmsContentService.Received(1).GetDisallowedContents("example.com");
        });
    }

    [Test]
    public async Task GetAsync_WhenGeneratedDisallowsExistWithoutWildcardGroup_CreatesWildcardGroup()
    {
        _cmsContentService.GetDisallowedContents("example.com").Returns(new[] { "/hidden" });

        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1",
            Files = new Dictionary<string, RobotsTxtFileOptions>
            {
                ["default"] = new()
                {
                    HostName = "example.com",
                    UserAgents = new Dictionary<string, RobotsTxtUserAgentOptions>
                    {
                        ["ai-bot"] = new() { Disallow = ["/bot"] }
                    }
                }
            }
        });

        var result = await sut.GetAsync("example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(2));
            Assert.That(result.Groups.Single(group => group.UserAgents.SequenceEqual(new[] { "*" })).Disallow, Is.EqualTo(new[] { "/hidden" }));
        });
    }

    private DefaultRobotsTxtService CreateService(RobotsTxtOptions options)
    {
        return new DefaultRobotsTxtService(Options.Create(options), _cmsContentService);
    }
}
