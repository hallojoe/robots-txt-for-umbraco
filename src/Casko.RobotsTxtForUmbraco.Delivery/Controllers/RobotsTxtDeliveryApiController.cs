using Asp.Versioning;
using Casko.RobotsTxtForUmbraco.Common;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Http;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Delivery.Authorization;
using Casko.RobotsTxtForUmbraco.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.Attributes;

namespace Casko.RobotsTxtForUmbraco.Delivery.Controllers;

[ApiExplorerSettings(GroupName = RobotsTxtApiConstants.ApiName)]
[ApiController]
[ApiVersion(RobotsTxtApiConstants.ApiVersion)]
[MapToApi($"{RobotsTxtApiConstants.ApiName}")]
[Route(RobotsTxtApiConstants.ApiRoute)]
[RobotsTxtDeliveryApiAccess]
public sealed class RobotsTxtDeliveryApiController(
    IOptions<RobotsTxtOptions> robotsTxtOptions,
    IRobotsTxtTextService robotsTxtTextService) : ControllerBase
{
    [Produces(Constants.TextMimeType)]
    [HttpGet("")]
    public async Task<IResult> GetRobotsTxt(
        [FromQuery(Name = "host")] string? hostName = null,
        [FromHeader(Name = RobotsTxtApiConstants.DeliveryApiKeyHeaderName)] string? apiKey = null
    )
    {
        if (robotsTxtOptions.Value.Enabled is not true)
        {
            return Results.NotFound();
        }

        try
        {
            var resolvedHostName = string.IsNullOrWhiteSpace(hostName)
                ? Request.Host.Value
                : hostName;

            var text = await robotsTxtTextService.GetTextAsync(resolvedHostName, HttpContext.RequestAborted);
        
            return new RobotsTxtResult(text);
        }
        catch (Exception exception)
        {
            return Results.BadRequest(exception.Message);
        }
    }
}
