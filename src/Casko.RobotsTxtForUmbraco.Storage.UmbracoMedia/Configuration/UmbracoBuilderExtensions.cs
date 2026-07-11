using Umbraco.Cms.Core.DependencyInjection;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddRobotsTxtUmbracoMediaStorage(this IUmbracoBuilder builder)
    {
        builder.Services.AddRobotsTxtUmbracoMediaStorage(builder.Config);
        
        return builder;
    }
}
