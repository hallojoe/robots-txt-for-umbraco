using Casko.RobotsTxtForUmbraco.Delivery.Configuration;
using Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Casko.RobotsTxtForUmbraco.Package;

public sealed class RobotsTxtComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddRobotsTxtDeliveryApi();
        builder.AddRobotsTxtUmbracoMediaStorage();
    }
}
