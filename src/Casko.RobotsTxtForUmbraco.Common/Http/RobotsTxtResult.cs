using System.Text;
using Microsoft.AspNetCore.Http;

namespace Casko.RobotsTxtForUmbraco.Common.Http;

public sealed class RobotsTxtResult(string content) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpContext.Response.WriteAsync(content, Encoding.UTF8);
    }
}
