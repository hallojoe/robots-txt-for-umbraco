using Casko.RobotsTxtForUmbraco.Common;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Casko.RobotsTxtForUmbraco.Delivery.Swagger;

public sealed class RobotsTxtDeliveryApiHeadersOperationFilter : IOperationFilter
{
    private const string ApiGroupName = RobotsTxtApiConstants.ApiName;
    private const string ApiKeyHeaderName = "Api-Key";
    private const string CultureHeaderName = "culture";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.GroupName, ApiGroupName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= [];

        AddHeaderParameter(operation, ApiKeyHeaderName, "Delivery API key.");

        var hasCultureHeader = context.ApiDescription.ParameterDescriptions.Any(parameter =>
            string.Equals(parameter.Name, CultureHeaderName, StringComparison.OrdinalIgnoreCase) &&
            parameter.Source?.Id == "Header");

        if (hasCultureHeader)
        {
            AddHeaderParameter(operation, CultureHeaderName, "Optional culture header used when resolving the sitemap path.");
        }
    }

    private static void AddHeaderParameter(OpenApiOperation operation, string headerName, string description)
    {
        operation.Parameters ??= [];
        var parameters = operation.Parameters;

        var alreadyExists = parameters.Any(parameter =>
            string.Equals(parameter.Name, headerName, StringComparison.OrdinalIgnoreCase) &&
            parameter.In == ParameterLocation.Header);

        if (alreadyExists)
        {
            return;
        }

        parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            In = ParameterLocation.Header,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}
