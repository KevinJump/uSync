using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Serialization.Serializers;

namespace uSync.Tests.Serializers;

/// <summary>
///  tests for importing templates - how we report failures, and how we get
///  the content for a template when there is no .cshtml file on disk.
/// </summary>
[TestFixture]
public class TemplateSerializerTests
{
    private Mock<ITemplateService> _templateServiceMock;
    private Mock<IUserIdKeyResolver> _userIdKeyResolverMock;
    private TemplateSerializer _serializer;

    [SetUp]
    public void Setup()
    {
        _templateServiceMock = new Mock<ITemplateService>();
        _userIdKeyResolverMock = new Mock<IUserIdKeyResolver>();
        _userIdKeyResolverMock.Setup(x => x.GetAsync(It.IsAny<int>())).ReturnsAsync(Guid.NewGuid());

        var versionMock = new Mock<IUmbracoVersion>();
        versionMock.Setup(x => x.Version).Returns(new Version(17, 3, 0));

        var fileSystems = BuildFileSystems();

        _serializer = new TemplateSerializer(
            Mock.Of<IEntityService>(),
            NullLogger<TemplateSerializer>.Instance,
            Mock.Of<IShortStringHelper>(),
            fileSystems,
            new ConfigurationBuilder().Build(),
            new uSyncCapabilityChecker(versionMock.Object),
            _templateServiceMock.Object,
            _userIdKeyResolverMock.Object);
    }

    private static FileSystems BuildFileSystems()
    {
        var hostingEnvironment = new Mock<IHostingEnvironment>();
#pragma warning disable CS0618 // used only to satisfy FileSystems' internal setup
        hostingEnvironment.Setup(x => x.MapPathContentRoot(It.IsAny<string>()))
            .Returns<string>(path => "C:/temp/" + path.TrimStart('~', '/'));
        hostingEnvironment.Setup(x => x.MapPathWebRoot(It.IsAny<string>()))
            .Returns<string>(path => "C:/temp/" + path.TrimStart('~', '/'));
#pragma warning restore CS0618
        hostingEnvironment.Setup(x => x.ToAbsolute(It.IsAny<string>()))
            .Returns<string>(path => "/" + path.TrimStart('~', '/'));

        var ioHelper = new Mock<IIOHelper>();
#pragma warning disable CS0618 // used only to satisfy FileSystems' internal setup
        ioHelper.Setup(x => x.ResolveUrl(It.IsAny<string>())).Returns<string>(path => "/" + path.TrimStart('~', '/'));
#pragma warning restore CS0618

        // PhysicalFileSystem uses this to check a path is inside its root - without it every
        // path looks like it's outside the root, and FileExists throws instead of returning false.
        ioHelper.Setup(x => x.PathStartsWith(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<char[]>()))
            .Returns((string path, string root, char[] separators)
                => path.StartsWith(root, StringComparison.OrdinalIgnoreCase));

        return new FileSystems(
            Mock.Of<ILoggerFactory>(),
            ioHelper.Object,
            Options.Create(new GlobalSettings()),
            hostingEnvironment.Object);
    }

    /// <summary>
    ///  build a template node as it would appear in a .config file. Contents is optional -
    ///  it's only in the file when the IncludeContent setting is on.
    /// </summary>
    private static XElement BuildTemplateNode(Guid key, string alias, string name, string parent = null, string contents = null)
    {
        var node = new XElement("Template",
            new XAttribute(uSyncConstants.Xml.Key, key),
            new XAttribute(uSyncConstants.Xml.Alias, alias),
            new XElement("Name", name),
            new XElement("Parent", parent ?? string.Empty));

        if (contents is not null)
            node.Add(new XElement("Contents", new XCData(contents)));

        return node;
    }

    /// <summary>
    ///  guards against #1044 - a failed template create used to be reported as a
    ///  hardcoded "Failed to create template" with no exception/status, hiding
    ///  why the import failed (e.g. only reproducible on IIS/Production).
    /// </summary>
    [Test]
    public async Task Create_WhenTemplateServiceFails_ReturnsStatusAndException()
    {
        // arrange: template doesn't exist locally, so the serializer will try to create it,
        // and Umbraco's ITemplateService reports it couldn't be created (e.g. duplicate alias).
        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((ITemplate)null);
        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((ITemplate)null);
        _templateServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Attempt<ITemplate, TemplateOperationStatus>.Fail(TemplateOperationStatus.DuplicateAlias));

        var node = BuildTemplateNode(Guid.NewGuid(), "linkTreeNew", "LinkTreeNew", contents: "@{ Layout = null; }");

        // act
        var result = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        // assert: the real Umbraco status and an exception now flow through, instead of a
        // generic message with no way to diagnose the underlying cause.
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain(nameof(TemplateOperationStatus.DuplicateAlias)));
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Exception.Message, Does.Contain(nameof(TemplateOperationStatus.DuplicateAlias)));
        });
    }

    [Test]
    public async Task Create_WhenTemplateServiceSucceeds_ReturnsSucceededAttempt()
    {
        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync((ITemplate)null);
        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((ITemplate)null);

        var created = new Template(Mock.Of<IShortStringHelper>(), "LinkTreeNew", "linkTreeNew");
        _templateServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Attempt<ITemplate, TemplateOperationStatus>.Succeed(TemplateOperationStatus.Success, created));

        var node = BuildTemplateNode(Guid.NewGuid(), "linkTreeNew", "LinkTreeNew", contents: "@{ Layout = null; }");

        var result = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.That(result.Success, Is.True);
    }

    #region No template file on disk

    /// <summary>
    ///  when the views are compiled into the site, there is no .cshtml on disk to read, so we
    ///  hand Umbraco a placeholder. Umbraco works the parent out by parsing the Layout value
    ///  out of that content, so the placeholder has to be something its parser understands.
    /// </summary>
    [Test]
    public async Task Create_WhenNoFileOnDiskAndTemplateHasParent_PlaceholderContentSetsTheMaster()
    {
        // arrange: no files on disk, and the views are compiled (razor views mode).
        var created = SetupTemplateServiceWithNoFilesOnDisk();
        var options = RazorViewOptions();

        // act: the parent has to exist before the child can reference it.
        var parentResult = await _serializer.DeserializeAsync(
            BuildTemplateNode(Guid.NewGuid(), "master", "Master"), options);

        var childResult = await _serializer.DeserializeAsync(
            BuildTemplateNode(Guid.NewGuid(), "childOfMaster", "Child Of Master", parent: "master"), options);

        // assert: both imported, and the content we sent for the child parses back to the parent
        // alias using Umbraco's own parser - which is how the master template actually gets set.
        var childContent = created["childOfMaster"];
        var parsedMaster = new TemplateContentParserService().LayoutTemplateAlias(childContent);

        Assert.Multiple(() =>
        {
            Assert.That(parentResult.Success, Is.True);
            Assert.That(childResult.Success, Is.True);
            Assert.That(parsedMaster, Is.EqualTo("master"));

            // and it's marked, so the second pass knows to delete the file Umbraco writes out.
            Assert.That(childContent, Does.Contain($"[uSyncMarker:{_serializer.Id}]"));
        });
    }

    /// <summary>
    ///  same path, but for a root template - the placeholder should tell Umbraco there is
    ///  no master, rather than pointing it at a template called "".
    /// </summary>
    [Test]
    public async Task Create_WhenNoFileOnDiskAndTemplateHasNoParent_PlaceholderContentSetsNoMaster()
    {
        var created = SetupTemplateServiceWithNoFilesOnDisk();

        var result = await _serializer.DeserializeAsync(
            BuildTemplateNode(Guid.NewGuid(), "master", "Master"), RazorViewOptions());

        var content = created["master"];
        var parsedMaster = new TemplateContentParserService().LayoutTemplateAlias(content);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(parsedMaster, Is.Null);
            Assert.That(content, Does.Contain($"[uSyncMarker:{_serializer.Id}]"));
        });
    }

    /// <summary>
    ///  when the views are not compiled a missing file is a genuine error - we can't create a
    ///  template from nothing. Umbraco's template service hands back Stream.Null (not null) for
    ///  a file that isn't there, so we have to spot that or we silently create empty templates.
    /// </summary>
    [Test]
    public async Task Create_WhenNoFileOnDiskAndViewsAreNotCompiled_FailsWithMissingFile()
    {
        SetupTemplateServiceWithNoFilesOnDisk();

        // note: no UsingRazorViews setting this time.
        var result = await _serializer.DeserializeAsync(
            BuildTemplateNode(Guid.NewGuid(), "orphan", "Orphan"), new SyncSerializerOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("missing"));
        });
    }

    /// <summary>
    ///  stands in for a site with no .cshtml files on disk (they are compiled into the site),
    ///  and remembers what we create so a child import can find its parent.
    /// </summary>
    /// <returns>the content we passed to the template service, keyed by alias.</returns>
    private Dictionary<string, string> SetupTemplateServiceWithNoFilesOnDisk()
    {
        var templates = new Dictionary<string, ITemplate>(StringComparer.InvariantCultureIgnoreCase);
        var content = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        // this is what Umbraco's TemplateRepository returns when the file isn't there.
        _templateServiceMock.Setup(x => x.GetFileContentStreamAsync(It.IsAny<string>()))
            .ReturnsAsync(Stream.Null);

        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((string alias) => templates.TryGetValue(alias, out var found) ? found : null);

        _templateServiceMock.Setup(x => x.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid key) =>
            {
                foreach (var template in templates.Values)
                {
                    if (template.Key == key) return template;
                }
                return null;
            });

        _templateServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string name, string alias, string templateContent, Guid userKey, Guid? key) =>
            {
                var template = new Template(Mock.Of<IShortStringHelper>(), name, alias)
                {
                    Content = templateContent
                };

                if (key is not null) template.Key = key.Value;

                templates[alias] = template;
                content[alias] = templateContent;

                return Attempt<ITemplate, TemplateOperationStatus>.Succeed(TemplateOperationStatus.Success, template);
            });

        return content;
    }

    private static SyncSerializerOptions RazorViewOptions()
        => new SyncSerializerOptions(new Dictionary<string, object>
        {
            [uSyncConstants.DefaultSettings.UsingRazorViews] = true
        });

    #endregion
}
