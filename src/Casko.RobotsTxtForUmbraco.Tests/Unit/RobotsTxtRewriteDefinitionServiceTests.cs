using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Delivery.Rewriting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtRewriteDefinitionServiceTests
{
    [Test]
    public void TryMatch_RewritesRobotsTxtToDeliveryApi()
    {
        var service = new RobotsTxtRewriteDefinitionService(Options.Create(new RobotsTxtOptions
        {
            Enabled = true,
            RewritesEnabled = true
        }));

        var matched = service.TryMatch("/robots.txt", new HostString("example.com"), out var targetPath);

        Assert.That(matched, Is.True);
        Assert.That(targetPath, Is.EqualTo("/umbraco/delivery/api/v1/robotstxt?host=example.com"));
    }

    [Test]
    public void TryMatch_DoesNotRewriteWhenDisabled()
    {
        var service = new RobotsTxtRewriteDefinitionService(Options.Create(new RobotsTxtOptions
        {
            Enabled = false,
            RewritesEnabled = true
        }));

        var matched = service.TryMatch("/robots.txt", new HostString("example.com"), out _);

        Assert.That(matched, Is.False);
    }
}
