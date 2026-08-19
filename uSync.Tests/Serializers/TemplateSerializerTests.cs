using System;
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
///  guards against #1044 - a failed template create used to be reported as a
///  hardcoded "Failed to create template" with no exception/status, hiding
///  why the import failed (e.g. only reproducible on IIS/Production).
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

        return new FileSystems(
            Mock.Of<ILoggerFactory>(),
            ioHelper.Object,
            Options.Create(new GlobalSettings()),
            hostingEnvironment.Object);
    }

    private static XElement BuildTemplateNode(Guid key, string alias, string name)
        => new XElement("Template",
            new XAttribute(uSyncConstants.Xml.Key, key),
            new XAttribute(uSyncConstants.Xml.Alias, alias),
            new XElement("Name", name),
            new XElement("Contents", new XCData("@{ Layout = null; }")));

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

        var node = BuildTemplateNode(Guid.NewGuid(), "linkTreeNew", "LinkTreeNew");

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

        var node = BuildTemplateNode(Guid.NewGuid(), "linkTreeNew", "LinkTreeNew");

        var result = await _serializer.DeserializeAsync(node, new SyncSerializerOptions());

        Assert.That(result.Success, Is.True);
    }
}
