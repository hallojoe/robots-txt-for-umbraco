using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Cms;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class DefaultRobotsTxtCmsContentServiceTests
{
    private IUmbracoContextFactory _umbracoContextFactory = null!;
    private IDocumentNavigationQueryService _documentNavigationQueryService = null!;
    private ILanguageService _languageService = null!;
    private IPublishedUrlProvider _publishedUrlProvider = null!;
    private IPublishedContentCache _publishedContentCache = null!;

    [SetUp]
    public void SetUp()
    {
        _umbracoContextFactory = Substitute.For<IUmbracoContextFactory>();
        _documentNavigationQueryService = Substitute.For<IDocumentNavigationQueryService>();
        _languageService = Substitute.For<ILanguageService>();
        _publishedUrlProvider = Substitute.For<IPublishedUrlProvider>();
        _publishedContentCache = Substitute.For<IPublishedContentCache>();

        var umbracoContext = Substitute.For<IUmbracoContext>();
        umbracoContext.Content.Returns(_publishedContentCache);

        var contextAccessor = Substitute.For<IUmbracoContextAccessor>();
        var contextReference = new UmbracoContextReference(umbracoContext, false, contextAccessor);
        _umbracoContextFactory.EnsureUmbracoContext().Returns(contextReference);
        _languageService.GetDefaultLanguageAsync().Returns(Task.FromResult<ILanguage?>(null));
        _languageService.GetAllAsync().Returns(Task.FromResult<IEnumerable<ILanguage>>([]));
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsZero_ReturnsDirectRoots()
    {
        var root = CreateContent(100, "home");
        ConfigureNavigationRoots(root);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { root }));
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsOne_ReturnsFirstLevelChildren()
    {
        var rootContainer = CreateContent(100, "container");
        var child = CreateContent(200, "home");
        ConfigureNavigationRoots(rootContainer);
        ConfigureChildRoots(rootContainer, child);
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 1 });

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { child }));
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsAboveOne_ThrowsClearException()
    {
        var root = CreateContent(100, "home");
        ConfigureNavigationRoots(root);
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 2 });

        TestDelegate action = () => _ = sut.GetRootContents().ToArray();

        Assert.That(
            action,
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo(
                    "The default IRobotsTxtCmsContentService implementation only supports RootNodeSearchLevel values 0 and 1. Configure a custom IRobotsTxtCmsContentService for deeper root structures."));
    }

    [Test]
    public void GetRootContents_WhenHostMatchesDirectRoot_ReturnsMatchedRootOnly()
    {
        var firstRoot = CreateContent(100, "home");
        var matchingRoot = CreateContent(101, "home");
        ConfigureNavigationRoots(firstRoot, matchingRoot);
        _publishedUrlProvider.GetUrl(firstRoot, UrlMode.Absolute, null, null).Returns("https://first.example.com/");
        _publishedUrlProvider.GetUrl(matchingRoot, UrlMode.Absolute, null, null).Returns("https://match.example.com/");
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents("match.example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { matchingRoot }));
    }

    [Test]
    public void GetRootContents_WhenHostMatchesChildRootAtLevelOne_ReturnsMatchedChildOnly()
    {
        var container = CreateContent(100, "container");
        var firstChild = CreateContent(200, "home");
        var matchingChild = CreateContent(201, "home");
        ConfigureNavigationRoots(container);
        ConfigureChildRoots(container, firstChild, matchingChild);
        _publishedUrlProvider.GetUrl(firstChild, UrlMode.Absolute, null, null).Returns("https://first.example.com/");
        _publishedUrlProvider.GetUrl(matchingChild, UrlMode.Absolute, null, null).Returns("https://match.example.com/");
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 1 });

        var result = sut.GetRootContents("https://match.example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { matchingChild }));
    }

    [Test]
    public void GetRootContents_WhenHostIsEmpty_ReturnsFirstResolvedRoots()
    {
        var firstRoot = CreateContent(100, "home");
        var secondRoot = CreateContent(101, "home");
        ConfigureNavigationRoots(firstRoot, secondRoot);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents(string.Empty).ToArray();

        Assert.That(result, Is.EqualTo(new[] { firstRoot, secondRoot }));
    }

    [Test]
    public void GetRootContents_WhenLevelOneHostDoesNotMatch_FallsBackToFirstChildRoot()
    {
        var container = CreateContent(100, "container");
        var firstChild = CreateContent(200, "home");
        var secondChild = CreateContent(201, "home");
        ConfigureNavigationRoots(container);
        ConfigureChildRoots(container, firstChild, secondChild);
        _publishedUrlProvider.GetUrl(firstChild, UrlMode.Absolute, null, null).Returns("https://first.example.com/");
        _publishedUrlProvider.GetUrl(secondChild, UrlMode.Absolute, null, null).Returns("https://second.example.com/");
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 1 });

        var result = sut.GetRootContents("unknown.example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { firstChild }));
    }

    [Test]
    public void GetDisallowedContents_WhenPropertyMatches_ReturnsNormalizedPaths()
    {
        var root = CreateContent(100, "home", excludingPropertyValue: "1");
        ConfigureNavigationRoots(root);
        ConfigureDescendants(root);
        _publishedUrlProvider.GetUrl(root, UrlMode.Absolute, Arg.Any<string>(), null).Returns("https://example.com/hidden");
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { "/hidden" }));
    }

    [Test]
    public void GetDisallowedContents_WhenHostDoesNotMatch_ReturnsEmpty()
    {
        var root = CreateContent(100, "home", excludingPropertyValue: "1");
        ConfigureNavigationRoots(root);
        ConfigureDescendants(root);
        _publishedUrlProvider.GetUrl(root, UrlMode.Absolute, Arg.Any<string>(), null).Returns("https://other.example.com/hidden");
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDisallowedContents_WhenIncludedContentTypeAliasesExcludeContent_ReturnsEmpty()
    {
        var root = CreateContent(100, "article", excludingPropertyValue: "1");
        ConfigureNavigationRoots(root);
        ConfigureDescendants(root);
        _publishedUrlProvider.GetUrl(root, UrlMode.Absolute, Arg.Any<string>(), null).Returns("https://example.com/hidden");
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1",
            IncludedContentTypeAliases = ["home"]
        });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDisallowedContents_WhenAliasIsBlank_ReturnsEmpty()
    {
        var root = CreateContent(100, "home", excludingPropertyValue: "1");
        ConfigureNavigationRoots(root);
        ConfigureDescendants(root);
        var sut = CreateService(new RobotsTxtOptions { ExcludingUrlPropertyAlias = string.Empty });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDisallowedContents_WhenDuplicatePathsExistAcrossCultures_ReturnsDistinctOrderedPaths()
    {
        var root = CreateContent(100, "home", excludingPropertyValue: "1", cultures: ["en-US", "da-DK"]);
        ConfigureNavigationRoots(root);
        ConfigureDescendants(root);
        _publishedUrlProvider.GetUrl(root, UrlMode.Absolute, Arg.Any<string>(), null).Returns("https://example.com/hidden");
        var defaultLanguage = CreateLanguage("en-US");
        var alternateLanguage = CreateLanguage("da-DK");
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });
        _languageService.GetDefaultLanguageAsync().Returns(Task.FromResult<ILanguage?>(defaultLanguage));
        _languageService.GetAllAsync().Returns(Task.FromResult<IEnumerable<ILanguage>>([defaultLanguage, alternateLanguage]));

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { "/hidden" }));
    }

    private DefaultRobotsTxtCmsContentService CreateService(RobotsTxtOptions options)
    {
        return new DefaultRobotsTxtCmsContentService(
            Options.Create(options),
            _umbracoContextFactory,
            _documentNavigationQueryService,
            _languageService,
            _publishedUrlProvider);
    }

    private void ConfigureNavigationRoots(params IPublishedContent[] roots)
    {
        _documentNavigationQueryService.TryGetRootKeys(out Arg.Any<IEnumerable<Guid>>())
            .Returns(callInfo =>
            {
                callInfo[0] = roots.Select(root => root.Key).ToArray();
                return true;
            });

        foreach (var root in roots)
        {
            _publishedContentCache.GetById(root.Key).Returns(root);
        }
    }

    private void ConfigureChildRoots(IPublishedContent parent, params IPublishedContent[] children)
    {
        _documentNavigationQueryService.TryGetChildrenKeys(parent.Key, out Arg.Any<IEnumerable<Guid>>())
            .Returns(callInfo =>
            {
                callInfo[1] = children.Select(child => child.Key).ToArray();
                return true;
            });

        foreach (var child in children)
        {
            _publishedContentCache.GetById(child.Key).Returns(child);
        }
    }

    private static void ConfigureDescendants(IPublishedContent root, params IPublishedContent[] descendants)
    {
        root.Children.Returns(descendants);
    }

    private static IPublishedContent CreateContent(
        int id,
        string contentTypeAlias,
        string? excludingPropertyValue = null,
        params string[] cultures)
    {
        var content = Substitute.For<IPublishedContent>();
        var contentType = Substitute.For<IPublishedContentType>();
        var property = Substitute.For<IPublishedProperty>();
        var contentKey = Guid.NewGuid();
        content.Id.Returns(id);
        content.Key.Returns(contentKey);
        contentType.Alias.Returns(contentTypeAlias);
        content.ContentType.Returns(contentType);

        if (excludingPropertyValue is not null)
        {
            property.GetValue(Arg.Any<string>(), null).Returns(excludingPropertyValue);
            content.GetProperty("umbracoNaviHide").Returns(property);
        }

        if (cultures.Length > 0)
        {
            content.Cultures.Returns(cultures.ToDictionary(culture => culture, _ => (PublishedCultureInfo)null!));
        }

        return content;
    }

    private static ILanguage CreateLanguage(string isoCode)
    {
        var language = Substitute.For<ILanguage>();
        language.IsoCode.Returns(isoCode);
        return language;
    }
}
