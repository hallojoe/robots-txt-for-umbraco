using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtOptionsTests
{
    [Test]
    public void Bind_BindsBindingsIncludeAndSitemaps()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RobotsTxtOptions.Key}:Hosts:example.com:Host"] = "example.com",
                [$"{RobotsTxtOptions.Key}:Hosts:example.com:Url"] = "https://www.example.com",
                [$"{RobotsTxtOptions.Key}:Hosts:example.com:Sitemaps:0"] = "/sitemap.xml",
                [$"{RobotsTxtOptions.Key}:Hosts:example.com:Include:0"] = "robots.base.txt"
            })
            .Build();

        var options = new RobotsTxtOptions();
        configuration.GetSection(RobotsTxtOptions.Key).Bind(options);

        Assert.Multiple(() =>
        {
            Assert.That(options.Hosts["example.com"].Url, Is.EqualTo("https://www.example.com"));
            Assert.That(options.Hosts["example.com"].Host, Is.EqualTo("example.com"));
            Assert.That(options.Hosts["example.com"].Sitemaps, Is.EqualTo(new[] { "/sitemap.xml" }));
            Assert.That(options.Hosts["example.com"].Include, Is.EqualTo(new[] { "robots.base.txt" }));
        });
    }

    [Test]
    public void Resolve_WhenHostMatches_ReturnsBindingAndConfiguredHostName()
    {
        var resolved = RobotsTxtOptionsResolver.Resolve(new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                ["binding-1"] = new() { Host = "https://example.com/", Include = ["robots.txt"] },
                [""] = new() { Include = ["robots.default.txt"] }
            }
        }, "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.HostName, Is.EqualTo("binding-1"));
            Assert.That(resolved.Binding.Include, Is.EqualTo(new[] { "robots.txt" }));
        });
    }

    [Test]
    public void Resolve_WhenHostDoesNotMatch_FallsBackToDefaultBinding()
    {
        var resolved = RobotsTxtOptionsResolver.Resolve(new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                [""] = new() { Include = ["robots.default.txt"] }
            }
        }, "unknown.example");

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.HostName, Is.Null);
            Assert.That(resolved.Binding.Include, Is.EqualTo(new[] { "robots.default.txt" }));
        });
    }

    [Test]
    public void GetConfiguredHostNames_ReturnsConfiguredBindingKeysAndDefault()
    {
        var result = RobotsTxtOptionsResolver.GetConfiguredHostNames(new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                [""] = new() { Include = ["robots.default.txt"] },
                ["binding-1"] = new() { Host = "example.com", Include = ["robots.host.txt"] }
            }
        });

        Assert.That(result, Is.EquivalentTo(new string?[] { "example.com", null }));
    }
}
