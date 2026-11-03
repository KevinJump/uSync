using System;
using System.Data;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Tests.Preview;

/// <summary>
///  previews can be built from a uSync file that isn't in the uSync folders (e.g. one in a sync pack),
///  and then the items it uses that aren't in Umbraco yet come from the same place.
/// </summary>
[TestFixture]
public class SyncPreviewServiceTests
{
    private static readonly Guid _contentTypeKey = Guid.NewGuid();
    private static readonly Guid _dataTypeKey = Guid.NewGuid();

    private Mock<ISyncHandler> _contentTypeHandler = null!;
    private Mock<ISyncHandler> _dataTypeHandler = null!;
    private Mock<ICoreScope> _scope = null!;
    private SyncPreviewService _service = null!;

    private readonly XElement _folderDataType = new("DataType", new XAttribute("Key", _dataTypeKey));
    private readonly object _folderDataTypeItem = new();

    [SetUp]
    public void Setup()
    {
        _dataTypeHandler = Handler("DataTypeHandler", nameof(IDataType));
        _dataTypeHandler.Setup(x => x.TryFindItemNodeAsync(_dataTypeKey)).ReturnsAsync(_folderDataType);
        _dataTypeHandler.Setup(x => x.GetPreviewAsync(It.IsAny<XElement>(), It.IsAny<HandlerSettings>(), It.IsAny<SyncPreviewOptions?>()))
            .ReturnsAsync((XElement node, HandlerSettings _, SyncPreviewOptions? _) => Preview(node, node == _folderDataType ? _folderDataTypeItem : node));

        // the content type uses a data type that isn't in Umbraco yet.
        _contentTypeHandler = Handler("ContentTypeHandler", nameof(IContentType));
        _contentTypeHandler.Setup(x => x.GetPreviewAsync(It.IsAny<XElement>(), It.IsAny<HandlerSettings>(), It.IsAny<SyncPreviewOptions?>()))
            .Returns(async (XElement node, HandlerSettings _, SyncPreviewOptions? options) =>
            {
                await options!.PendingItemResolver!(typeof(IDataType), _dataTypeKey);
                return Preview(node, new object());
            });

        var factory = new Mock<ISyncHandlerFactory>();
        factory.Setup(x => x.GetValidHandler("ContentTypeHandler", It.IsAny<SyncHandlerOptions?>())).Returns(Pair(_contentTypeHandler));
        factory.Setup(x => x.GetValidHandlers(It.IsAny<SyncHandlerOptions?>())).Returns([Pair(_contentTypeHandler), Pair(_dataTypeHandler)]);

        _scope = new Mock<ICoreScope> { DefaultValue = DefaultValue.Mock };
        var scopeProvider = new Mock<ICoreScopeProvider>();
        scopeProvider.Setup(x => x.CreateCoreScope(
            It.IsAny<IsolationLevel>(), It.IsAny<RepositoryCacheMode>(), It.IsAny<IEventDispatcher?>(),
            It.IsAny<IScopedNotificationPublisher?>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(_scope.Object);

        _service = new SyncPreviewService(factory.Object, scopeProvider.Object,
            new SyncPreviewMapperCollection(() => []), NullLogger<SyncPreviewService>.Instance);
    }

    [Test]
    public async Task Builds_Preview_From_The_Node_It_Is_Given()
    {
        var packNode = new XElement("ContentType", new XAttribute("Key", _contentTypeKey));

        var result = await _service.GetPreviewAsync("ContentTypeHandler", packNode, (_, _) => Task.FromResult<XElement?>(null));

        Assert.That(result?.Preview.Node, Is.SameAs(packNode));
        _contentTypeHandler.Verify(x => x.TryFindItemNodeAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Pending_Items_Come_From_The_Finder()
    {
        var packDataType = new XElement("DataType", new XAttribute("Key", _dataTypeKey));
        ISyncHandler? askedHandler = null;

        var result = await _service.GetPreviewAsync("ContentTypeHandler", new XElement("ContentType"), (handler, key) =>
        {
            askedHandler = handler;
            return Task.FromResult<XElement?>(key == _dataTypeKey ? packDataType : null);
        });

        Assert.That(askedHandler, Is.SameAs(_dataTypeHandler.Object));
        Assert.That(result?.Preview.PendingItems, Is.EqualTo(new object[] { packDataType }));
        _dataTypeHandler.Verify(x => x.TryFindItemNodeAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Pending_Items_Come_From_The_uSync_Folders_Without_A_Finder()
    {
        var result = await _service.GetPreviewAsync("ContentTypeHandler", new XElement("ContentType"));

        Assert.That(result?.Preview.PendingItems, Is.EqualTo(new[] { _folderDataTypeItem }));
    }

    [Test]
    public async Task Preview_Scope_Is_Never_Completed()
    {
        await _service.GetPreviewAsync("ContentTypeHandler", new XElement("ContentType"), (_, _) => Task.FromResult<XElement?>(null));

        _scope.Verify(x => x.Complete(), Times.Never);
    }

    [Test]
    public async Task Unknown_Handler_Has_No_Preview()
    {
        var result = await _service.GetPreviewAsync("NotAHandler", new XElement("ContentType"));

        Assert.That(result, Is.Null);
    }

    private static Mock<ISyncHandler> Handler(string alias, string itemType)
    {
        var handler = new Mock<ISyncHandler>();
        handler.Setup(x => x.Alias).Returns(alias);
        handler.Setup(x => x.ItemType).Returns(itemType);
        return handler;
    }

    private static HandlerConfigPair Pair(Mock<ISyncHandler> handler)
        => new() { Handler = handler.Object, Settings = new HandlerSettings() };

    // the incoming item stands in for what the serializer would build from the node.
    private static SyncItemPreview Preview(XElement node, object incoming)
        => new() { Key = Guid.Empty, Handler = "test", ItemType = "test", Node = node, Incoming = incoming, Success = true };
}
