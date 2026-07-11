using Microsoft.AspNetCore.Http;

namespace Casko.RobotsTxtForUmbraco.Delivery.Rewriting;

public interface IRobotsTxtRewriteDefinitionService
{
    bool TryMatch(PathString requestPath, HostString requestHost, out string? targetPath);
}
