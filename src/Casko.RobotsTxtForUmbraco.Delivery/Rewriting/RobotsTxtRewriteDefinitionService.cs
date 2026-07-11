using Casko.RobotsTxtForUmbraco.Common;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Delivery.Rewriting;

public sealed class RobotsTxtRewriteDefinitionService(IOptions<RobotsTxtOptions> options) : IRobotsTxtRewriteDefinitionService
{
    public bool TryMatch(PathString requestPath, HostString requestHost, out string? targetPath)
    {
        targetPath = null;
        if (!options.Value.Enabled || !options.Value.RewritesEnabled)
        {
            return false;
        }

        if (!string.Equals(requestPath.Value, "/robots.txt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        targetPath = $"/{RobotsTxtApiConstants.ApiRoute}?host={Uri.EscapeDataString(requestHost.Value ?? string.Empty)}";
        return true;
    }
}
