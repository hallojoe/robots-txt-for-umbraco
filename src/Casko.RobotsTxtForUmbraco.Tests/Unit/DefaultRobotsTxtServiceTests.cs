using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class DefaultRobotsTxtServiceTests
{
    [Test]
    public async Task GetAsync_WhenHostMatches_LoadsAndMergesIncludedFiles()
    {
        var options = new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                ["example.com"] = new()
                {
                    Host = "example.com",
                    Url = "https://www.example.com",
                    Sitemaps = ["/binding-sitemap.xml"],
                    Include = ["robots.base.txt", "robots.extra.txt"]
                }
            }
        };

        var bindingFileResolver = Substitute.For<IRobotsTxtBindingFileResolver>();
        bindingFileResolver.ReadAsync("robots.base.txt", Arg.Any<CancellationToken>())
            .Returns("User-agent: *\nDisallow: /private");
        bindingFileResolver.ReadAsync("robots.extra.txt", Arg.Any<CancellationToken>())
            .Returns("User-agent: *\nAllow: /public\nSitemap: /from-file.xml");

        var result = await CreateService(options, bindingFileResolver).GetAsync("example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.SitemapBaseUrl, Is.EqualTo("https://www.example.com"));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "/from-file.xml", "/binding-sitemap.xml" }));
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "*" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }

    [Test]
    public async Task GetAsync_WhenNoHostMatches_FallsBackToDefaultBinding()
    {
        var options = new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                [""] = new()
                {
                    Include = ["robots.default.txt"]
                }
            }
        };

        var bindingFileResolver = Substitute.For<IRobotsTxtBindingFileResolver>();
        bindingFileResolver.ReadAsync("robots.default.txt", Arg.Any<CancellationToken>())
            .Returns("User-agent: *\nDisallow: /");

        var result = await CreateService(options, bindingFileResolver).GetAsync("unknown.example");

        Assert.Multiple(() =>
        {
            Assert.That(result.SitemapBaseUrl, Is.Null);
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/" }));
        });
    }

    [Test]
    public async Task GetAsync_WhenBindingUrlIsEmpty_UsesBindingHostNameAsSitemapBaseUrl()
    {
        var options = new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                ["localhost:44346"] = new()
                {
                    Host = "localhost:44346",
                    Url = "",
                    Sitemaps = ["/sitemap.xml"],
                    Include = []
                }
            }
        };

        var result = await CreateService(options, Substitute.For<IRobotsTxtBindingFileResolver>()).GetAsync("localhost:44346");

        Assert.Multiple(() =>
        {
            Assert.That(result.SitemapBaseUrl, Is.EqualTo("https://localhost:44346"));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "/sitemap.xml" }));
        });
    }

    private static DefaultRobotsTxtService CreateService(
        RobotsTxtOptions options,
        IRobotsTxtBindingFileResolver bindingFileResolver)
    {
        return new DefaultRobotsTxtService(
            Options.Create(options),
            new RobotsTxtRenderer(),
            bindingFileResolver);
    }
}
