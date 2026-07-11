using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DeliveryApi;

namespace Casko.RobotsTxtForUmbraco.Delivery.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RobotsTxtDeliveryApiAccessAttribute() : TypeFilterAttribute(typeof(RobotsTxtDeliveryApiFilter))
{
    private sealed class RobotsTxtDeliveryApiFilter(
        IOptions<RobotsTxtOptions> settings,
        IApiAccessService apiAccessService,
        IRequestPreviewService requestPreviewService) : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!settings.Value.UseDeliveryApiAccessPolicy)
            {
                return;
            }

            var hasAccess = requestPreviewService.IsPreview()
                ? apiAccessService.HasPreviewAccess()
                : apiAccessService.HasPublicAccess();

            if (!hasAccess)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
