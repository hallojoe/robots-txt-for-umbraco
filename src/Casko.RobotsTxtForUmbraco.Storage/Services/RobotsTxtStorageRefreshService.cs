using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Casko.RobotsTxtForUmbraco.Common.Services.Rendering;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Storage.Services;

public sealed class RobotsTxtStorageRefreshService(
    IRobotsTxtService robotsTxtService,
    IRobotsTxtRenderer renderer,
    IRobotsTxtDataSource dataSource,
    IOptions<RobotsTxtOptions> options) : IRobotsTxtStorageRefreshService
{
    /// <inheritdoc />
    public async Task<string> RefreshAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        var document = await robotsTxtService.GetAsync(hostName, cancellationToken);
        var text = renderer.Render(document);
        await dataSource.WriteAsync(new RobotsTxtStorageKey(hostName), text, cancellationToken);
        return text;
    }

    /// <inheritdoc />
    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        var configuredHosts = options.Value.Files.Count == 0
            ? [null]
            : options.Value.Files.Values
                .Select(file => file.HostName)
                .Append(null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        foreach (var hostName in configuredHosts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshAsync(hostName, cancellationToken);
        }
    }
}
