using Casko.RobotsTxtForUmbraco.Storage;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class RobotsTxtStorageNameProviderTests
{
    [Test]
    public void GetFileName_UsesHostSafeName()
    {
        var provider = new RobotsTxtStorageNameProvider();

        var fileName = provider.GetFileName(new RobotsTxtStorageKey("Example.com:443"));

        Assert.That(fileName, Is.EqualTo("robots-example.com-443.txt"));
    }
}
