using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Cms;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRobotsTxt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RobotsTxtOptions>(configuration.GetSection(RobotsTxtOptions.Key));

        services.AddScoped<IRobotsTxtCmsContentService, ExamineRobotsTxtCmsContentService>();
        services.AddScoped<IRobotsTxtContentCollector, RobotsTxtContentCollector>();
        services.AddScoped<IRobotsTxtRenderer, RobotsTxtRenderer>();
        services.AddScoped<IRobotsTxtService, DefaultRobotsTxtService>();
        services.AddScoped<IRobotsTxtTextService, DefaultRobotsTxtTextService>();

        return services;
    }
}
