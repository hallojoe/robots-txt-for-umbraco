using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Delivery.Configuration;
using Casko.RobotsTxtForUmbraco.Storage.Configuration;
using Casko.RobotsTxtForUmbraco.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class ServiceRegistrationTests
{
    [Test]
    public void AddRobotsTxtDeliveryApi_WhenStorageWasAddedFirst_KeepsStoredTextServiceRegistration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RobotsTxtOptions.Key}:Enabled"] = "true",
                [$"{RobotsTxtOptions.Key}:RewritesEnabled"] = "true"
            })
            .Build();

        services.AddRobotsTxtStorage(configuration);
        services.AddRobotsTxtDeliveryApi(configuration, addRewritePipeline: false);

        var descriptor = services.Last(service => service.ServiceType == typeof(IRobotsTxtTextService));

        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(StoredRobotsTxtTextService)));
    }
}
