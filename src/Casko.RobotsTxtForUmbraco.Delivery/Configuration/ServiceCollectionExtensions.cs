using Casko.RobotsTxtForUmbraco.Common;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Delivery.Controllers;
using Casko.RobotsTxtForUmbraco.Delivery.Rewriting;
using Casko.RobotsTxtForUmbraco.Delivery.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Casko.RobotsTxtForUmbraco.Delivery.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxtDeliveryApi(
        this IServiceCollection services,
        IConfiguration configuration,
        bool addRewritePipeline = true)
    {
        var robotsTxtConfigurationSection = configuration.GetSection(RobotsTxtOptions.Key);
        var robotsTxtOptions = robotsTxtConfigurationSection.Get<RobotsTxtOptions>();

        if (robotsTxtOptions?.Enabled is not true)
        {
            return services;
        }

        services.AddRobotsTxt(configuration);
        
        services
            .AddControllers()
            .AddApplicationPart(typeof(RobotsTxtDeliveryApiController).Assembly);
        
        services.ConfigureOptions<RobotsTxtApiConfigureSwaggerGenOptions>();

        services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                $"{RobotsTxtApiConstants.ApiName}-controllers",
                endpoints: app => app.UseEndpoints(endpoints => endpoints.MapControllers())));

            // options.AddFilter(new UmbracoPipelineFilter(
            //     $"{RobotsTxtApiConstants.ApiName}-response-headers",
            //     prePipeline: app => app.UseMiddleware<HttpResponseHeadersMiddleware>()));
        });

        if (addRewritePipeline)
        {
            services.AddRobotsTxtRewritePipeline(configuration);
        }

        return services;
    }

    private static void AddRobotsTxtRewritePipeline(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new RobotsTxtOptions();
        configuration.GetSection(RobotsTxtOptions.Key).Bind(settings);

        if (!settings.Enabled || !settings.RewritesEnabled)
        {
            return;
        }

        services.AddSingleton<IRobotsTxtRewriteDefinitionService, RobotsTxtRewriteDefinitionService>();

        services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                RobotsTxtApiConstants.RewritesKey,
                prePipeline: app => app.UseMiddleware<RobotsTxtRewriteMiddleware>()));
        });
    }
}
