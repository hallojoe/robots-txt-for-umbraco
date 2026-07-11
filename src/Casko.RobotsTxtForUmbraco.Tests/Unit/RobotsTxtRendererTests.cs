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

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: *" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine +
            "Allow: /public" + Environment.NewLine +
            Environment.NewLine +
            "Sitemap: https://example.com/sitemap.xml" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenSitemapIsRelative_ExpandsItUsingSitemapBaseUrl()
    {
        var document = new RobotsTxtDocument
        {
            SitemapBaseUrl = "https://example.com",
            Sitemaps = ["/sitemap.xml"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "Sitemap: https://example.com/sitemap.xml" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenSitemapIsAlreadyAbsolute_LeavesItUnchanged()
    {
        var document = new RobotsTxtDocument
        {
            SitemapBaseUrl = "https://example.com",
            Sitemaps = ["https://external.com/sitemap.xml"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "Sitemap: https://external.com/sitemap.xml" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenSitemapBaseUrlIsMissing_LeavesRelativeSitemapUnchanged()
    {
        var document = new RobotsTxtDocument
        {
            Sitemaps = ["other-sitemap.xml"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "Sitemap: other-sitemap.xml" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenSitemapBaseUrlIsInvalid_LeavesRelativeSitemapUnchanged()
    {
        var document = new RobotsTxtDocument
        {
            SitemapBaseUrl = "not a url",
            Sitemaps = ["other-sitemap.xml"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "Sitemap: other-sitemap.xml" + Environment.NewLine));
    }

    [TestCase("OpenAI-Bot, Google-Bot")]
    [TestCase("OpenAI-Bot;Google-Bot")]
    public void Render_WhenUserAgentKeyContainsMultipleAgents_WritesOneLinePerAgent(string userAgentValue)
    {
        var document = new RobotsTxtDocument
        {
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = [userAgentValue],
                    Disallow = ["/private"]
                }
            ]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: OpenAI-Bot" + Environment.NewLine +
            "User-agent: Google-Bot" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenUserAgentContainsSpaces_KeepsTheNameIntact()
    {
        var document = new RobotsTxtDocument
        {
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = ["Brightbot 1.0"],
                    Disallow = ["/private"]
                }
            ]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: Brightbot 1.0" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenDisallowUserAgentsConfigured_WritesLeadingDisallowBlock()
    {
        var document = new RobotsTxtDocument
        {
            DisallowUserAgents = ["GPTBot", "ChatGPT-user"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: GPTBot" + Environment.NewLine +
            "User-agent: ChatGPT-user" + Environment.NewLine +
            "Disallow: /" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenDisallowUserAgentsConfigured_WritesLeadingBlockBeforeNormalGroups()
    {
        var document = new RobotsTxtDocument
        {
            DisallowUserAgents = ["GPTBot"],
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = ["*"],
                    Disallow = ["/private"]
                }
            ]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: GPTBot" + Environment.NewLine +
            "Disallow: /" + Environment.NewLine +
            Environment.NewLine +
            "User-agent: *" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenDisallowUserAgentsConfigured_WritesLeadingBlockBeforeSitemaps()
    {
        var document = new RobotsTxtDocument
        {
            DisallowUserAgents = ["GPTBot"],
            Sitemaps = ["https://example.com/sitemap.xml"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: GPTBot" + Environment.NewLine +
            "Disallow: /" + Environment.NewLine +
            Environment.NewLine +
            "Sitemap: https://example.com/sitemap.xml" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenDisallowUserAgentAlsoExistsInGroups_OmitsItFromLaterGroups()
    {
        var document = new RobotsTxtDocument
        {
            DisallowUserAgents = ["GPTBot"],
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = ["GPTBot, Google-Bot"],
                    Disallow = ["/private"]
                }
            ]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: GPTBot" + Environment.NewLine +
            "Disallow: /" + Environment.NewLine +
            Environment.NewLine +
            "User-agent: Google-Bot" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine));
    }

    [Test]
    public void Render_WhenDisallowUserAgentContainsSpaces_KeepsTheNameIntact()
    {
        var document = new RobotsTxtDocument
        {
            DisallowUserAgents = ["Brightbot 1.0"]
        };

        var result = CreateRenderer().Render(document);

        Assert.That(result, Is.EqualTo(
            "User-agent: Brightbot 1.0" + Environment.NewLine +
            "Disallow: /" + Environment.NewLine));
    }

    [Test]
    public void Parse_ParsesStandardDocument()
    {
        const string raw =
            "User-agent: *\n" +
            "Allow: /public\n" +
            "Disallow: /private\n" +
            "Sitemap: https://example.com/sitemap.xml\n";

        var result = CreateRenderer().Parse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "*" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "https://example.com/sitemap.xml" }));
        });
    }

    [Test]
    public void Parse_WhenMultipleUserAgentsShareRules_ParsesIntoSingleGroup()
    {
        const string raw =
            "User-agent: OpenAI-Bot\n" +
            "User-agent: Google-Bot\n" +
            "Disallow: /private\n";

        var result = CreateRenderer().Parse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "OpenAI-Bot", "Google-Bot" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }

    [Test]
    public void Parse_IgnoresCommentsBlankLinesAndUnknownDirectives()
    {
        const string raw =
            "# comment\n" +
            "\n" +
            "Host: example.com\n" +
            "User-agent: * # inline comment\n" +
            "Disallow: /private\n" +
            "Sitemap: https://example.com/sitemap.xml # comment\n";

        var result = CreateRenderer().Parse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "*" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "https://example.com/sitemap.xml" }));
        });
    }

    [Test]
    public void Parse_IgnoresOrphanRulesBeforeUserAgent()
    {
        const string raw =
            "Disallow: /private\n" +
            "Allow: /public\n" +
            "User-agent: *\n" +
            "Disallow: /real\n";

        var result = CreateRenderer().Parse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/real" }));
            Assert.That(result.Groups[0].Allow, Is.Empty);
        });
    }

    [Test]
    public void Parse_ParsesMixedCaseDirectives()
    {
        const string raw =
            "uSeR-aGeNt: *\n" +
            "aLlOw: /public\n" +
            "dIsAlLoW: /private\n" +
            "SiTeMaP: https://example.com/sitemap.xml\n";

        var result = CreateRenderer().Parse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "https://example.com/sitemap.xml" }));
        });
    }

    [Test]
    public void Merge_MergesWildcardGroups()
    {
        var document1 = new RobotsTxtDocument
        {
            Groups = [new RobotsTxtGroup { UserAgents = ["*"], Disallow = ["/private"] }]
        };
        var document2 = new RobotsTxtDocument
        {
            Groups = [new RobotsTxtGroup { UserAgents = ["*"], Allow = ["/public"] }]
        };

        var result = CreateRenderer().Merge(document1, document2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "*" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }

    [Test]
    public void Merge_MergesEquivalentMultiAgentGroups()
    {
        var document1 = new RobotsTxtDocument
        {
            Groups = [new RobotsTxtGroup { UserAgents = ["OpenAI-Bot, Google-Bot"], Disallow = ["/private"] }]
        };
        var document2 = new RobotsTxtDocument
        {
            Groups =
            [
                new RobotsTxtGroup
                {
                    UserAgents = ["Google-Bot", "OpenAI-Bot"],
                    Allow = ["/public"]
                }
            ]
        };

        var result = CreateRenderer().Merge(document1, document2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].UserAgents, Is.EqualTo(new[] { "Google-Bot", "OpenAI-Bot" }));
            Assert.That(result.Groups[0].Allow, Is.EqualTo(new[] { "/public" }));
            Assert.That(result.Groups[0].Disallow, Is.EqualTo(new[] { "/private" }));
        });
    }

    [Test]
    public void Merge_KeepsDistinctGroupsSeparate()
    {
        var document1 = new RobotsTxtDocument
        {
            Groups = [new RobotsTxtGroup { UserAgents = ["*"], Disallow = ["/private"] }]
        };
        var document2 = new RobotsTxtDocument
        {
            Groups = [new RobotsTxtGroup { UserAgents = ["bingbot"], Allow = ["/public"] }]
        };

        var result = CreateRenderer().Merge(document1, document2);

        Assert.That(result.Groups.Count, Is.EqualTo(2));
    }

    [Test]
    public void Merge_MergesSitemapsAndKeepsFirstBaseUrl()
    {
        var document1 = new RobotsTxtDocument
        {
            SitemapBaseUrl = "https://one.example.com",
            Sitemaps = ["/sitemap.xml", "/sitemap.xml"]
        };
        var document2 = new RobotsTxtDocument
        {
            SitemapBaseUrl = "https://two.example.com",
            Sitemaps = ["/other-sitemap.xml"]
        };

        var result = CreateRenderer().Merge(document1, document2);

        Assert.Multiple(() =>
        {
            Assert.That(result.SitemapBaseUrl, Is.EqualTo("https://one.example.com"));
            Assert.That(result.Sitemaps, Is.EqualTo(new[] { "/sitemap.xml", "/other-sitemap.xml" }));
        });
    }

    [Test]
    public void Merge_UsesSecondBaseUrlWhenFirstIsMissing()
    {
        var document1 = new RobotsTxtDocument();
        var document2 = new RobotsTxtDocument
        {
            SitemapBaseUrl = "https://two.example.com"
        };

        var result = CreateRenderer().Merge(document1, document2);

        Assert.That(result.SitemapBaseUrl, Is.EqualTo("https://two.example.com"));
    }

    [Test]
    public void Render_AfterParse_ProducesNormalizedOutput()
    {
        const string raw =
            "# comment\n" +
            "User-agent: OpenAI-Bot, Google-Bot\n" +
            "Disallow: /private\n" +
            "Disallow: /private\n" +
            "Sitemap: https://example.com/sitemap.xml\n" +
            "Sitemap: https://example.com/sitemap.xml\n";

        var renderer = CreateRenderer();
        var result = renderer.Render(renderer.Parse(raw));

        Assert.That(result, Is.EqualTo(
            "User-agent: OpenAI-Bot" + Environment.NewLine +
            "User-agent: Google-Bot" + Environment.NewLine +
            "Disallow: /private" + Environment.NewLine +
            Environment.NewLine +
            "Sitemap: https://example.com/sitemap.xml" + Environment.NewLine));
    }

    private static RobotsTxtRenderer CreateRenderer()
    {
        return new RobotsTxtRenderer();
    }
}
