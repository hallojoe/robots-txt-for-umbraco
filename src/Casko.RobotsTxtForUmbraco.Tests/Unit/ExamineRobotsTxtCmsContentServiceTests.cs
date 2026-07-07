using Casko.RobotsTxtForUmbraco.Common.Configuration;
using Casko.RobotsTxtForUmbraco.Common.Services.Cms;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Examine;

namespace Casko.RobotsTxtForUmbraco.Tests.Unit;

[TestFixture]
public sealed class ExamineRobotsTxtCmsContentServiceTests
{
    private IExamineManager _examineManager = null!;
    private IIndex _index = null!;
    private ISearcher _searcher = null!;
    private IQuery _publishedQuery = null!;
    private IBooleanOperation _publishedOperation = null!;
    private IQuery _levelQuery = null!;
    private IBooleanOperation _levelOperation = null!;
    private IQuery _hostQuery = null!;
    private IBooleanOperation _hostOperation = null!;
    private IBooleanOperation _disallowedOperation = null!;
    private IUmbracoContextFactory _umbracoContextFactory = null!;
    private ILanguageService _languageService = null!;
    private IPublishedContentCache _publishedContentCache = null!;

    [SetUp]
    public void SetUp()
    {
        _examineManager = Substitute.For<IExamineManager>();
        _index = Substitute.For<IIndex>();
        _searcher = Substitute.For<ISearcher>();
        _publishedQuery = Substitute.For<IQuery>();
        _publishedOperation = Substitute.For<IBooleanOperation>();
        _levelQuery = Substitute.For<IQuery>();
        _levelOperation = Substitute.For<IBooleanOperation>();
        _hostQuery = Substitute.For<IQuery>();
        _hostOperation = Substitute.For<IBooleanOperation>();
        _disallowedOperation = Substitute.For<IBooleanOperation>();
        _umbracoContextFactory = Substitute.For<IUmbracoContextFactory>();
        _languageService = Substitute.For<ILanguageService>();
        _publishedContentCache = Substitute.For<IPublishedContentCache>();

        _index.Searcher.Returns(_searcher);
        _searcher.CreateQuery(IndexTypes.Content, BooleanOperation.And).Returns(_publishedQuery);
        _publishedQuery.Field("__Published", "y").Returns(_publishedOperation);
        _publishedOperation.And().Returns(_levelQuery);
        _levelQuery.Field<int>("level", Arg.Any<int>()).Returns(_levelOperation);
        _levelQuery.Field(Arg.Any<string>(), Arg.Any<IExamineValue>()).Returns(_disallowedOperation);
        _levelOperation.And().Returns(_hostQuery);
        _hostQuery.Field("publishedHost", Arg.Any<IExamineValue>()).Returns(_hostOperation);
        _disallowedOperation.And().Returns(_hostQuery);

        var umbracoContext = Substitute.For<IUmbracoContext>();
        umbracoContext.Content.Returns(_publishedContentCache);

        var contextAccessor = Substitute.For<IUmbracoContextAccessor>();
        var contextReference = new UmbracoContextReference(umbracoContext, false, contextAccessor);
        _umbracoContextFactory.EnsureUmbracoContext().Returns(contextReference);

        ConfigureIndexAvailable();
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsZero_ReturnsDirectRoots()
    {
        var root = CreateContent(100, "home");
        ConfigureBaseResults(CreateResult(100, parentId: -1, sortOrder: 2));
        _publishedContentCache.GetById(100).Returns(root);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { root }));
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsOne_ReturnsFirstLevelRoots()
    {
        var firstChild = CreateContent(200, "home");
        var secondChild = CreateContent(201, "home");
        ConfigureBaseResults(
            CreateResult(201, parentId: 101, sortOrder: 2),
            CreateResult(200, parentId: 100, sortOrder: 1));
        _publishedContentCache.GetById(200).Returns(firstChild);
        _publishedContentCache.GetById(201).Returns(secondChild);
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 1 });

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { firstChild, secondChild }));
    }

    [Test]
    public void GetRootContents_WhenRootNodeSearchLevelIsAboveOne_ThrowsClearException()
    {
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 2 });

        TestDelegate action = () => _ = sut.GetRootContents().ToArray();

        Assert.That(
            action,
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo(
                    "The default IRobotsTxtCmsContentService implementation only supports RootNodeSearchLevel values 0 and 1. Configure a custom IRobotsTxtCmsContentService for deeper root structures."));
    }

    [Test]
    public void GetRootContents_WhenHostMatches_ReturnsMatchedRootOnly()
    {
        var matchingRoot = CreateContent(101, "home");
        ConfigureHostResults(CreateResult(101, parentId: -1, sortOrder: 2));
        _publishedContentCache.GetById(101).Returns(matchingRoot);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents("https://match.example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { matchingRoot }));
    }

    [Test]
    public void GetRootContents_WhenLevelOneHostDoesNotMatch_FallsBackToFirstOrderedCandidate()
    {
        var firstChild = CreateContent(200, "home");
        var secondChild = CreateContent(201, "home");
        ConfigureHostResults();
        ConfigureBaseResults(
            CreateResult(201, parentId: 100, sortOrder: 9),
            CreateResult(200, parentId: 100, sortOrder: 1));
        _publishedContentCache.GetById(200).Returns(firstChild);
        _publishedContentCache.GetById(201).Returns(secondChild);
        var sut = CreateService(new RobotsTxtOptions { RootNodeSearchLevel = 1 });

        var result = sut.GetRootContents("unknown.example.com").ToArray();

        Assert.That(result, Is.EqualTo(new[] { firstChild }));
    }

    [Test]
    public void GetRootContents_WhenLevelZeroHostDoesNotMatch_ReturnsEmpty()
    {
        ConfigureHostResults();
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents("unknown.example.com").ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetRootContents_WhenExternalIndexIsMissing_ReturnsEmpty()
    {
        _examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out Arg.Any<IIndex?>())
            .Returns(false);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetRootContents_WhenSearchResultCannotBeResolvedToPublishedContent_SkipsMissingItems()
    {
        var existing = CreateContent(201, "home");
        ConfigureBaseResults(
            CreateResult(200, parentId: -1, sortOrder: 1),
            CreateResult(201, parentId: -1, sortOrder: 2));
        _publishedContentCache.GetById(200).Returns((IPublishedContent?)null);
        _publishedContentCache.GetById(201).Returns(existing);
        var sut = CreateService(new RobotsTxtOptions());

        var result = sut.GetRootContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { existing }));
    }

    [Test]
    public void GetDisallowedContents_WhenAliasAndValueMatch_ReturnsPaths()
    {
        ConfigureDisallowedResults(CreateResult(101, path: "/hidden"));
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { "/hidden" }));
    }

    [Test]
    public void GetDisallowedContents_WhenHostIsProvided_UsesPublishedHostField()
    {
        ConfigureDisallowedHostResults(CreateResult(101, path: "/hidden"));
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new[] { "/hidden" }));
            _hostQuery.Received(1).Field("publishedHost", Arg.Any<IExamineValue>());
        });
    }

    [Test]
    public void GetDisallowedContents_WhenIndexIsMissing_ReturnsEmpty()
    {
        _examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out Arg.Any<IIndex?>())
            .Returns(false);
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents("example.com").ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDisallowedContents_WhenHitDoesNotContainPublishedUrlPath_SkipsIt()
    {
        ConfigureDisallowedResults(CreateResult(101));
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents().ToArray();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetDisallowedContents_WhenDuplicatePathsExist_ReturnsDistinctOrderedPaths()
    {
        ConfigureDisallowedResults(
            CreateResult(101, path: "/z-hidden"),
            CreateResult(102, path: "/a-hidden"),
            CreateResult(103, path: "/a-hidden"));
        var sut = CreateService(new RobotsTxtOptions
        {
            ExcludingUrlPropertyAlias = "umbracoNaviHide",
            ExcludingUrlPropertyValue = "1"
        });

        var result = sut.GetDisallowedContents().ToArray();

        Assert.That(result, Is.EqualTo(new[] { "/a-hidden", "/z-hidden" }));
    }

    private ExamineRobotsTxtCmsContentService CreateService(RobotsTxtOptions options)
    {
        return new ExamineRobotsTxtCmsContentService(
            Options.Create(options),
            _examineManager,
            _umbracoContextFactory,
            _languageService);
    }

    private void ConfigureIndexAvailable()
    {
        _examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out Arg.Any<IIndex?>())
            .Returns(callInfo =>
            {
                callInfo[1] = _index;
                return true;
            });
    }

    private void ConfigureBaseResults(params ISearchResult[] results)
    {
        _levelOperation.Execute().Returns(CreateResults(results));
    }

    private void ConfigureHostResults(params ISearchResult[] results)
    {
        _hostOperation.Execute().Returns(CreateResults(results));
    }

    private void ConfigureDisallowedResults(params ISearchResult[] results)
    {
        _disallowedOperation.Execute().Returns(CreateResults(results));
    }

    private void ConfigureDisallowedHostResults(params ISearchResult[] results)
    {
        _hostOperation.Execute().Returns(CreateResults(results));
    }

    private static LuceneSearchResults CreateResults(params ISearchResult[] results)
    {
        return new LuceneSearchResults(results, results.Length, float.NaN, null);
    }

    private static ISearchResult CreateResult(int id, int parentId = -1, int sortOrder = 0, string? path = null)
    {
        var result = Substitute.For<ISearchResult>();
        result.Id.Returns(id.ToString());
        result["parentID"].Returns(parentId.ToString());
        result["sortOrder"].Returns(sortOrder.ToString());
        result["id"].Returns(id.ToString());
        result.Values.Returns(path is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>
            {
                ["publishedUrlPath"] = path
            });
        return result;
    }

    private static IPublishedContent CreateContent(int id, string contentTypeAlias)
    {
        var content = Substitute.For<IPublishedContent>();
        var contentType = Substitute.For<IPublishedContentType>();
        content.Id.Returns(id);
        contentType.Alias.Returns(contentTypeAlias);
        content.ContentType.Returns(contentType);
        return content;
    }
}
