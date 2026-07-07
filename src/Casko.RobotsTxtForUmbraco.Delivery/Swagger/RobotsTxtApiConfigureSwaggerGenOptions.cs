using Casko.RobotsTxtForUmbraco.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Casko.RobotsTxtForUmbraco.Delivery.Swagger;

public sealed class RobotsTxtApiConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        if (!options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(RobotsTxtApiConstants.ApiName))
        {
            options.SwaggerDoc($"{RobotsTxtApiConstants.ApiName}", new OpenApiInfo
            {
                Title = RobotsTxtApiConstants.ApiTitle,
                Version = RobotsTxtApiConstants.ApiVersion
            });
        }

        options.OperationFilter<RobotsTxtDeliveryApiHeadersOperationFilter>();
    }
}
