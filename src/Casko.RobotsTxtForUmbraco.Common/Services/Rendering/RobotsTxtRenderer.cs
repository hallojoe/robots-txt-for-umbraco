using System.Text;
using Casko.RobotsTxtForUmbraco.Models;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Rendering;

public sealed class RobotsTxtRenderer : IRobotsTxtRenderer
{
    /// <inheritdoc />
    public string Render(RobotsTxtDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        var wroteBlock = false;

        foreach (var group in document.Groups.Where(group => group.UserAgents.Count > 0))
        {
            if (wroteBlock)
            {
                builder.AppendLine();
            }

            foreach (var userAgent in DistinctClean(group.UserAgents))
            {
                builder.Append("User-agent: ").AppendLine(userAgent);
            }

            foreach (var allow in DistinctClean(group.Allow))
            {
                builder.Append("Allow: ").AppendLine(allow);
            }

            foreach (var disallow in DistinctClean(group.Disallow))
            {
                builder.Append("Disallow: ").AppendLine(disallow);
            }

            wroteBlock = true;
        }

        var sitemaps = DistinctClean(document.Sitemaps).ToArray();
        if (sitemaps.Length > 0)
        {
            if (wroteBlock)
            {
                builder.AppendLine();
            }

            foreach (var sitemap in sitemaps)
            {
                builder.Append("Sitemap: ").AppendLine(sitemap);
            }
        }

        return builder.ToString();
    }

    private static IEnumerable<string> DistinctClean(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
