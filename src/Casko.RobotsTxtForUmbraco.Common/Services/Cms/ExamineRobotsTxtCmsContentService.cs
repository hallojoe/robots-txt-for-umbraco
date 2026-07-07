using Examine;
using Examine.Search;
using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Casko.RobotsTxtForUmbraco.Common.Services.Cms;

public sealed class ExamineRobotsTxtCmsContentService(
    IOptions<RobotsTxtOptions> options,
    IExamineManager examineManager,
    IUmbracoContextFactory umbracoContextFactory,
    ILanguageService languageService) : IRobotsTxtCmsContentService
{
    private const string NodeTypeAliasField = "__NodeTypeAlias";
    private const string PublishedField = "__Published";
    private const string PublishedHostField = "publishedHost";
    private const string PublishedUrlPathField = "publishedUrlPath";
    private const string LevelField = "level";
    private const string ParentIdField = "parentID";
    private const string SortOrderField = "sortOrder";
    private const string IdField = "id";

    /// <inheritdoc />
    public IEnumerable<IPublishedContent> GetRootContents(string? hostName = null)
    {
        if (!examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            return [];
        }

        var rootNodeSearchLevel = GetConfiguredRootNodeSearchLevel();
        var query = BuildBaseQuery(index.Searcher, rootNodeSearchLevel);
        var typedAliases = GetHostingDocumentTypeAliases();

        if (typedAliases.Length > 0)
        {
            query = query
                .And()
                .GroupedOr([NodeTypeAliasField], typedAliases);
        }

        var normalizedHostName = RobotsTxtHostName.Normalize(hostName);
        if (!string.IsNullOrWhiteSpace(normalizedHostName))
        {
            var hostMatchedRoots = ResolveContents(
                query.And().Field(PublishedHostField, normalizedHostName.Escape()).Execute());

            if (hostMatchedRoots.Length > 0)
            {
                return [hostMatchedRoots[0]];
            }

            return rootNodeSearchLevel == 1
                ? ResolveContents(query.Execute()).Take(1).ToArray()
                : [];
        }

        return ResolveContents(query.Execute());
    }

    /// <inheritdoc />
    public IEnumerable<string> GetDisallowedContents(string? hostName = null)
    {
        if (!examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            return [];
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ExcludingUrlPropertyAlias))
        {
            return [];
        }

        var normalizedHostName = RobotsTxtHostName.Normalize(hostName);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldSet in GetDisallowedFieldSets(settings))
        {
            var query = index.Searcher
                .CreateQuery(IndexTypes.Content, BooleanOperation.And)
                .Field(PublishedField, "y")
                .And()
                .Field(fieldSet.ExcludingFieldName, (settings.ExcludingUrlPropertyValue ?? string.Empty).Escape());

            if (!string.IsNullOrWhiteSpace(normalizedHostName))
            {
                query = query
                    .And()
                    .Field(fieldSet.HostFieldName, normalizedHostName.Escape());
            }

            foreach (var result in query.Execute())
            {
                var path = result.Values.TryGetValue(fieldSet.PathFieldName, out var values)
                    ? values?.ToString()
                    : null;

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                paths.Add(NormalizePath(path));
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

    private IBooleanOperation BuildBaseQuery(ISearcher searcher, int rootNodeSearchLevel)
    {
        return searcher
            .CreateQuery(IndexTypes.Content, BooleanOperation.And)
            .Field(PublishedField, "y")
            .And()
            .Field(LevelField, ResolveExamineLevel(rootNodeSearchLevel));
    }

    private int GetConfiguredRootNodeSearchLevel() => options.Value.RootNodeSearchLevel;

    private IEnumerable<DisallowedFieldSet> GetDisallowedFieldSets(RobotsTxtOptions settings)
    {
        var alias = settings.ExcludingUrlPropertyAlias;
        if (string.IsNullOrWhiteSpace(alias))
        {
            yield break;
        }

        yield return new DisallowedFieldSet(alias, PublishedHostField, PublishedUrlPathField);

        foreach (var culture in ResolveCultures(settings))
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                continue;
            }

            var suffix = culture.ToLowerInvariant();
            yield return new DisallowedFieldSet($"{alias}_{suffix}", $"{PublishedHostField}_{suffix}", $"{PublishedUrlPathField}_{suffix}");
        }
    }

    private string[] GetHostingDocumentTypeAliases()
    {
        var aliases = options.Value.HostingDocumentTypeAliases;
        if (aliases.Length > 0)
        {
            return aliases;
        }

        return options.Value.RootDocumentTypeAliases;
    }

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

        return filteredCultures;
    }

    private static int ResolveExamineLevel(int rootNodeSearchLevel)
    {
        return rootNodeSearchLevel switch
        {
            0 => 1,
            1 => 2,
            _ => throw new InvalidOperationException(
                "The default IRobotsTxtCmsContentService implementation only supports RootNodeSearchLevel values 0 and 1. Configure a custom IRobotsTxtCmsContentService for deeper root structures.")
        };
    }

    private IPublishedContent[] ResolveContents(ISearchResults results)
    {
        var orderedResults = results
            .OrderBy(result => GetNumericValue(result, ParentIdField))
            .ThenBy(result => GetNumericValue(result, SortOrderField))
            .ThenBy(result => GetNumericValue(result, IdField))
            .ToArray();

        using UmbracoContextReference umbracoContextReference = umbracoContextFactory.EnsureUmbracoContext();
        var publishedContentCache = umbracoContextReference.UmbracoContext.Content;

        return orderedResults
            .Select(result => ParseKey(result.Id))
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Select(publishedContentCache.GetById)
            .WhereNotNull()
            .ToArray();
    }

    private static int GetNumericValue(ISearchResult result, string fieldName)
    {
        return int.TryParse(result[fieldName], out var value)
            ? value
            : int.MaxValue;
    }

    private static int? ParseKey(string value)
    {
        return int.TryParse(value, out var id)
            ? id
            : null;
    }

    private static string NormalizePath(string value)
    {
        var path = value.Split('#')[0];
        return path.StartsWith('/') ? path : "/" + path;
    }

    private sealed record DisallowedFieldSet(string ExcludingFieldName, string HostFieldName, string PathFieldName);
}
