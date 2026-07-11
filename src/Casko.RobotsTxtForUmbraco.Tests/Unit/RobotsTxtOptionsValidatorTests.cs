using Casko.RobotsTxtForUmbraco.Common.Configuration;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtOptionsValidatorTests
{
    [Test]
    public void Validate_WhenDefaultHostIsUnknown_ReturnsFailure()
    {
        var result = new RobotsTxtOptionsValidator().Validate(null, new RobotsTxtOptions
        {
            DefaultHost = "missing",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
            }
        });

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_WhenProfileReferenceIsUnknown_ReturnsFailure()
    {
        var result = new RobotsTxtOptionsValidator().Validate(null, new RobotsTxtOptions
        {
            DefaultHost = "default",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
                {
                    Profiles = ["missing-profile"]
                }
            }
        });

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_WhenHostsAndProfilesAreValid_ReturnsSuccess()
    {
        var result = new RobotsTxtOptionsValidator().Validate(null, new RobotsTxtOptions
        {
            DefaultHost = "default",
            Hosts = new Dictionary<string, RobotsTxtHostOptions>
            {
                ["default"] = new()
                {
                    Profiles = ["profile-1"]
                }
            },
            Profiles = new Dictionary<string, RobotsTxtProfileOptions>
            {
                ["profile-1"] = new()
            }
        });

        Assert.That(result.Succeeded, Is.True);
    }
}
