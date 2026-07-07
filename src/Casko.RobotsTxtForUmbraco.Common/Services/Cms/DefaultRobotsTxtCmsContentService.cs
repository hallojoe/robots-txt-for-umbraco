using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Cms;

public sealed class DefaultRobotsTxtCmsContentService(
    IOptions<RobotsTxtOptions> options,
    IUmbracoContextFactory umbracoContextFactory,
    IDocumentNavigationQueryService documentNavigationQueryService,
    ILanguageService languageService,
    IPublishedUrlProvider publishedUrlProvider) : IRobotsTxtCmsContentService
{
    /// <inheritdoc />
    public IEnumerable<IPublishedContent> GetRootContents(string? hostName = null)
    {
        using var umbracoContextReference = umbracoContextFactory.EnsureUmbracoContext();
        if (!documentNavigationQueryService.TryGetRootKeys(out var rootKeys))
        {
            return [];
        }

        var navigationRoots = rootKeys
            .Select(key => umbracoContextReference.UmbracoContext.Content.GetById(key))
            .WhereNotNull()
            .ToArray();

        var siteRoots = GetConfiguredRootNodeSearchLevel() switch
        {
            0 => navigationRoots,
            1 => navigationRoots.SelectMany(root =>
                GetChildContents(root.Key, umbracoContextReference.UmbracoContext.Content)).ToArray(),
            _ => throw new InvalidOperationException(
                "The default IRobotsTxtCmsContentService implementation only supports RootNodeSearchLevel values 0 and 1. Configure a custom IRobotsTxtCmsContentService for deeper root structures.")
        };

        if (string.IsNullOrWhiteSpace(hostName))
        {
            return siteRoots;
        }

        var matchedRoot = siteRoots.FirstOrDefault(root => IsHostnameMatch(hostName, root));
        if (matchedRoot is not null)
        {
            return [matchedRoot];
        }

        return GetConfiguredRootNodeSearchLevel() == 1 && siteRoots.Length > 0
            ? [siteRoots[0]]
            : [];
    }

    /// <inheritdoc />
    public IEnumerable<string> GetDisallowedContents(string? hostName = null)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ExcludingUrlPropertyAlias))
        {
            return [];
        }

        var cultures = ResolveCultures(settings);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var content = GetRootContents(hostName)
            .SelectMany(GetDescendantsAndSelf);

        foreach (var item in content)
        {
            if (!ShouldConsiderContent(item, settings) || !IsExcludedByProperty(item, settings, cultures))
            {
                continue;
            }

            foreach (var culture in cultures)
            {
                var path = GetMatchingPath(item, culture, hostName);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }
        }

        return paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetLanguagesAsync()
    {
        var defaultLanguageCode = (await languageService.GetDefaultLanguageAsync())?.IsoCode;
        var allLanguageCodes = (await languageService.GetAllAsync())
            .Select(language => language.IsoCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(defaultLanguageCode))
        {
            allLanguageCodes.RemoveAll(code => string.Equals(code, defaultLanguageCode, StringComparison.OrdinalIgnoreCase));
            allLanguageCodes.Insert(0, defaultLanguageCode);
        }

        return allLanguageCodes;
    }

    private int GetConfiguredRootNodeSearchLevel() => options.Value.RootNodeSearchLevel;

    private IReadOnlyCollection<string> ResolveCultures(RobotsTxtOptions settings)
    {
        var defaultLanguageCode = languageService.GetDefaultLanguageAsync().GetAwaiter().GetResult()?.IsoCode;
        var cultures = languageService.GetAllAsync()
            .GetAwaiter()
            .GetResult()
            .Select(language => language.IsoCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(defaultLanguageCode))
        {
            cultures.RemoveAll(code => string.Equals(code, defaultLanguageCode, StringComparison.OrdinalIgnoreCase));
            cultures.Insert(0, defaultLanguageCode);
        }

        IReadOnlyCollection<string> filteredCultures = cultures;

        if (settings.IncludedCultures.Count > 0)
        {
            filteredCultures = filteredCultures
                .Where(culture => settings.IncludedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        if (settings.ExcludedCultures.Count > 0)
        {
            filteredCultures = filteredCultures
                .Where(culture => !settings.ExcludedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        return filteredCultures.Count > 0 ? filteredCultures : [string.Empty];
    }

    private IEnumerable<IPublishedContent> GetChildContents(Guid parentKey, IPublishedContentCache publishedContentCache)
    {
        if (!documentNavigationQueryService.TryGetChildrenKeys(parentKey, out IEnumerable<Guid> childKeys))
        {
            return [];
        }

        return childKeys
            .Select(key => publishedContentCache.GetById(key))
            .WhereNotNull();
    }

    private IEnumerable<IPublishedContent> GetDescendantsAndSelf(IPublishedContent root)
    {
        yield return root;

        foreach (var child in root.Children)
        {
            foreach (var descendant in GetDescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private string? GetMatchingPath(IPublishedContent content, string culture, string? hostName)
    {
        var absoluteUrl = publishedUrlProvider.GetUrl(content, UrlMode.Absolute, culture, current: null);
        if (string.IsNullOrWhiteSpace(absoluteUrl) || absoluteUrl == "#")
        {
            return null;
        }

        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return NormalizePath(absoluteUrl);
        }

        var normalizedHost = RobotsTxtHostName.Normalize(hostName);
        if (!string.IsNullOrWhiteSpace(normalizedHost) &&
            !string.Equals(RobotsTxtHostName.Normalize(uri.Authority), normalizedHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
    }

    private bool IsHostnameMatch(string hostName, IPublishedContent rootContent)
    {
        var rootContentUrl = publishedUrlProvider.GetUrl(rootContent, UrlMode.Absolute, culture: null, current: null);
        return RobotsTxtHostName.IsMatchAgainstAbsoluteUrl(hostName, rootContentUrl);
    }

    private static bool ShouldConsiderContent(IPublishedContent content, RobotsTxtOptions settings)
    {
        var alias = content.ContentType.Alias;
        if (settings.IncludedContentTypeAliases.Count > 0 &&
            !settings.IncludedContentTypeAliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return !settings.ExcludedContentTypeAliases.Contains(alias, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsExcludedByProperty(
        IPublishedContent content,
        RobotsTxtOptions settings,
        IReadOnlyCollection<string> cultures)
    {
        var alias = settings.ExcludingUrlPropertyAlias;
        if (string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        var property = content.GetProperty(alias);
        if (property is null)
        {
            return false;
        }

        return cultures.Any(culture =>
            string.Equals(
                property.GetValue(culture, segment: null)?.ToString(),
                settings.ExcludingUrlPropertyValue,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string value)
    {
        var path = value.Split('#')[0];
        return path.StartsWith('/') ? path : "/" + path;
    }
}
