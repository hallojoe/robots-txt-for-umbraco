using Umbraco.Cms.Core.DependencyInjection;

namespace Casko.RobotsTxtForUmbraco.Delivery.Configuration;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddRobotsTxtDeliveryApi(this IUmbracoBuilder builder)
    {
        builder.Services.AddRobotsTxtDeliveryApi(builder.Config);
        return builder;
    }
}
