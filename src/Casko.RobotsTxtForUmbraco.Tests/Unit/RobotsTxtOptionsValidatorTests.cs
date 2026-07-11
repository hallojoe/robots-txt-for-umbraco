using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtOptionsValidatorTests
{
    [Test]
    public void Validate_WhenIncludeEntryIsBlank_ReturnsFailure()
    {
        using var fixture = CreateFixture();

        var result = fixture.Validator.Validate(null, new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                ["example.com"] = new()
                {
                    Include = [" "]
                }
            }
        });

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_WhenIncludeFileIsMissing_ReturnsFailure()
    {
        using var fixture = CreateFixture();

        var result = fixture.Validator.Validate(null, new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                ["example.com"] = new()
                {
                    Include = ["robots.missing.txt"]
                }
            }
        });

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void Validate_WhenBindingsAreValid_ReturnsSuccess()
    {
        using var fixture = CreateFixture(("robots.valid.txt", "User-agent: *\nDisallow: /private"));

        var result = fixture.Validator.Validate(null, new RobotsTxtOptions
        {
            Hosts = new Dictionary<string, RobotsTxtBindingOptions>
            {
                [""] = new()
                {
                    Include = ["robots.valid.txt"]
                }
            }
        });

        Assert.That(result.Succeeded, Is.True);
    }

    private static TestFixtureScope CreateFixture(params (string FileName, string Contents)[] files)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"robots-txt-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        foreach (var file in files)
        {
            File.WriteAllText(Path.Combine(rootPath, file.FileName), file.Contents);
        }

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(rootPath);

        var fileResolver = new ContentRootRobotsTxtBindingFileResolver(hostEnvironment);
        return new TestFixtureScope(rootPath, new RobotsTxtOptionsValidator(fileResolver));
    }

    private sealed class TestFixtureScope(string rootPath, RobotsTxtOptionsValidator validator) : IDisposable
    {
        public RobotsTxtOptionsValidator Validator { get; } = validator;

        public void Dispose()
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }
}
