using Examine;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Casko.RobotsTxtForUmbraco.Common.Components;

public sealed class ExternalIndexHostAndPathFieldsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<ExternalIndexPublishedHostAndPathFieldsComponent>();
    }
}

public sealed class ExternalIndexPublishedHostAndPathFieldsComponent(
    IExamineManager examineManager,
    IUmbracoContextFactory umbracoContextFactory,
    IPublishedUrlProvider publishedUrlProvider,
    IDomainService domainService,
    ILanguageService languageService,
    ILogger<ExternalIndexPublishedHostAndPathFieldsComponent> logger)
    : IAsyncComponent
{
    private const string PublishedUrlPathField = "publishedUrlPath";
    private const string PublishedHostField = "publishedHost";

    private Dictionary<int, string>? _languageIsoById;

    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (!examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            logger.LogWarning("Could not find Examine index {IndexName}", Constants.UmbracoIndexes.ExternalIndexName);

            return Task.CompletedTask;
        }

        index.TransformingIndexValues += OnTransformingIndexValues;    
        
        return Task.CompletedTask;
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            index.TransformingIndexValues -= OnTransformingIndexValues;
        }
        
        return Task.CompletedTask;
    }
    
    private void OnTransformingIndexValues(object? sender, IndexingItemEventArgs e)
    {
        if (e.ValueSet.Category != IndexTypes.Content)
        {
            return;
        }

        if (!int.TryParse(e.ValueSet.Id, out int contentId))
        {
            return;
        }

        using UmbracoContextReference contextReference = umbracoContextFactory.EnsureUmbracoContext();

        IPublishedContent? content = contextReference.UmbracoContext.Content.GetById(contentId);
        if (content is null)
        {
            return;
        }

        Dictionary<string, IEnumerable<object>> values = e.ValueSet.Values.ToDictionary(
            x => x.Key,
            x => x.Value.AsEnumerable());

        string[] cultures = GetCultures(content);

        if (cultures.Length == 0)
        {
            AddInvariantFields(content, values);
        }
        else
        {
            foreach (string culture in cultures)
            {
                AddCultureFields(content, culture, values);
            }
        }

        e.SetValues(values);
    }

    private void AddInvariantFields(
        IPublishedContent content,
        Dictionary<string, IEnumerable<object>> values)
    {
        string? path = GetUrlPath(content, culture: null);
        if (!string.IsNullOrWhiteSpace(path))
        {
            values[PublishedUrlPathField] = [path];
        }

        string[] hosts = GetHosts(content, culture: null);
        if (hosts.Length > 0)
        {
            values[PublishedHostField] = hosts;
        }
    }

    private void AddCultureFields(
        IPublishedContent content,
        string culture,
        Dictionary<string, IEnumerable<object>> values)
    {
        string cultureSuffix = culture.ToLowerInvariant();

        string? path = GetUrlPath(content, culture);
        if (!string.IsNullOrWhiteSpace(path))
        {
            values[$"{PublishedUrlPathField}_{cultureSuffix}"] = [path];
        }

        string[] hosts = GetHosts(content, culture);
        if (hosts.Length > 0)
        {
            values[$"{PublishedHostField}_{cultureSuffix}"] = hosts;
        }
    }

    private string[] GetCultures(IPublishedContent content)
    {
        if (content.Cultures.Count == 0)
        {
            return [];
        }

        return content.Cultures
            .Keys
            .Where(culture => !string.IsNullOrWhiteSpace(culture))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(culture => culture)
            .ToArray();
    }

    private string? GetUrlPath(IPublishedContent content, string? culture)
    {
        string url = content.Url(publishedUrlProvider, culture, UrlMode.Relative);

        if (string.IsNullOrWhiteSpace(url) || url == "#")
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.AbsolutePath;
        }

        return url.StartsWith('/') ? url : "/" + url;
    }

    private string[] GetHosts(IPublishedContent content, string? culture)
    {
        IPublishedContent root = content.Root();

        Dictionary<int, string> languageIsoById = GetLanguageIsoById();

        // TODO: fix
        return domainService
            .GetAssignedDomains(root.Id, includeWildcards: true)
            .Where(domain =>
            {
                if (culture is null)
                {
                    return true;
                }

                if (domain.LanguageId is null)
                {
                    return false;
                }

                return languageIsoById.TryGetValue(domain.LanguageId.Value, out string? iso)
                       && iso.Equals(culture, StringComparison.OrdinalIgnoreCase);
            })
            .Select(domain => NormalizeHost(domain.DomainName))
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(host => host)
            .ToArray();
    }

    private Dictionary<int, string> GetLanguageIsoById()
    {
        return _languageIsoById ??= languageService
            .GetAllAsync()
            .GetAwaiter()
            .GetResult()
            .ToDictionary(language => language.Id, language => language.IsoCode);
    }

    private static string NormalizeHost(string domainName)
    {
        string value = domainName.Trim();

        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = "https://" + value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return uri.Authority;
        }

        return domainName.Trim('/');
    }


}