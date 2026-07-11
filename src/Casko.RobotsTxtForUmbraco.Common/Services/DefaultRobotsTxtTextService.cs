using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;

namespace Casko.RobotsTxtForUmbraco.Common.Services;

public sealed class DefaultRobotsTxtTextService(
    IRobotsTxtService robotsTxtService,
    IRobotsTxtRenderer robotsTxtRenderer) : IRobotsTxtTextService
{
    /// <inheritdoc />
    public async Task<string> GetTextAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        var document = await robotsTxtService.GetAsync(hostName, cancellationToken);
        return robotsTxtRenderer.Render(document);
    }
}
