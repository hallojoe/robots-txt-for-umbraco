using Microsoft.AspNetCore.Http;

namespace Casko.RobotsTxtForUmbraco.Delivery.Rewriting;

public sealed class RobotsTxtRewriteMiddleware(
    RequestDelegate next,
    IRobotsTxtRewriteDefinitionService rewriteDefinitionService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (rewriteDefinitionService.TryMatch(context.Request.Path, context.Request.Host, out var targetPath) &&
            !string.IsNullOrWhiteSpace(targetPath))
        {
            RewriteRequest(context, targetPath);
        }

        await next(context);
    }

    private static void RewriteRequest(HttpContext context, string targetPath)
    {
        var querySeparatorIndex = targetPath.IndexOf('?', StringComparison.Ordinal);
        if (querySeparatorIndex < 0)
        {
            context.Request.Path = targetPath;
            context.Request.QueryString = QueryString.Empty;
            return;
        }

        context.Request.Path = targetPath[..querySeparatorIndex];
        context.Request.QueryString = QueryString.FromUriComponent(targetPath[querySeparatorIndex..]);
    }
}
