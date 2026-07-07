using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Storage.Services;

public sealed class StoredRobotsTxtTextService(
    IRobotsTxtDataSource dataSource,
    IRobotsTxtStorageRefreshService refreshService,
    IOptions<RobotsTxtOptions> options,
    TimeProvider timeProvider) : IRobotsTxtTextService
{
    /// <inheritdoc />
    public async Task<string> GetTextAsync(string? hostName, CancellationToken cancellationToken = default)
    {
        var key = new RobotsTxtStorageKey(hostName);
        var storedDocument = await dataSource.ReadAsync(key, cancellationToken);
        if (storedDocument is not null && !IsStale(storedDocument))
        {
            return storedDocument.Text;
        }

        return await refreshService.RefreshAsync(hostName, cancellationToken);
    }

    private bool IsStale(RobotsTxtStoredDocument storedDocument)
    {
        var staleAfterSeconds = options.Value.Storage.RefreshStaleAfterSeconds;
        if (staleAfterSeconds <= 0)
        {
            return false;
        }

        if (storedDocument.RefreshedUtc is null)
        {
            return true;
        }

        return storedDocument.RefreshedUtc.Value.AddSeconds(staleAfterSeconds) <= timeProvider.GetUtcNow();
    }
}
