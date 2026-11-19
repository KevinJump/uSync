using System;
using System.Xml.Linq;

using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;
using Umbraco.Cms.Infrastructure.Serialization;

using uSync.BackOffice;
using uSync.BackOffice.WebhookEvents;
using uSync.Core;

namespace uSync.Tests.WebhookEvents;

[TestFixture]
internal class WebhookPayloadSerializationTests
{
    private IWebhookFiringService _firingService;
    private IWebhookService _webhookService;
    private IOptionsMonitor<WebhookSettings> _webhookSettings;
    private IServerRoleAccessor _serverRoleAccessor;
    private SystemTextWebhookJsonSerializer _serializer;

    [SetUp]
    public void Setup()
    {
        _firingService = Mock.Of<IWebhookFiringService>();
        _webhookService = Mock.Of<IWebhookService>();
        _webhookSettings = Mock.Of<IOptionsMonitor<WebhookSettings>>(x => x.CurrentValue == new WebhookSettings());
        _serverRoleAccessor = Mock.Of<IServerRoleAccessor>();
        _serializer = new SystemTextWebhookJsonSerializer(new DefaultJsonSerializerEncoderFactory());
    }

    [Test]
    public void ItemImported_Serializes()
    {
        var key = Guid.NewGuid();
        var notification = new uSyncImportedItemNotification(CreateItem(key), ChangeType.Import);
        var webhookEvent = new uSyncItemImportedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertItemJson(json, key, "Import");
    }

    [Test]
    public void ItemExported_Serializes()
    {
        var key = Guid.NewGuid();
        var notification = new uSyncExportedItemNotification(CreateItem(key), ChangeType.Export);
        var webhookEvent = new uSyncItemExportedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertItemJson(json, key, "Export");
    }

    [Test]
    public void ItemImported_WithoutAttributes_Serializes()
    {
        var notification = new uSyncImportedItemNotification(new XElement("ContentType", new XElement("Info")), ChangeType.Import);
        var webhookEvent = new uSyncItemImportedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"key\":null"));
            Assert.That(json, Does.Contain("\"alias\":null"));
            Assert.That(json, Does.Contain("\"itemType\":\"ContentType\""));
            Assert.That(json, Does.Contain("\"change\":\"Import\""));
        });
    }

    [Test]
    public void ItemExported_NullItem_Serializes()
    {
        var notification = new uSyncExportedItemNotification(null, ChangeType.Export);
        var webhookEvent = new uSyncItemExportedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"key\":null"));
            Assert.That(json, Does.Contain("\"alias\":null"));
            Assert.That(json, Does.Contain("\"itemType\":null"));
            Assert.That(json, Does.Contain("\"change\":\"Export\""));
        });
    }

    [Test]
    public void ImportCompleted_WithThrownException_Serializes()
    {
        var notification = new uSyncImportCompletedNotification([CreateFailAction(GetThrownException())], "settings");
        var webhookEvent = new uSyncImportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertCompletedJson(json, "boom");
    }

    [Test]
    public void ImportCompleted_WithInnerException_Serializes()
    {
        var notification = new uSyncImportCompletedNotification(
            [CreateFailAction(new Exception("outer", GetThrownException()))], "settings");
        var webhookEvent = new uSyncImportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertCompletedJson(json, "outer");
    }

    [Test]
    public void ExportCompleted_WithThrownException_Serializes()
    {
        var notification = new uSyncExportCompletedNotification([CreateFailAction(GetThrownException())], "settings");
        var webhookEvent = new uSyncExportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertCompletedJson(json, "boom");
    }

    [Test]
    public void ImportCompleted_NoFailures_Serializes()
    {
        var action = uSyncAction.SetAction(true, "home", "ContentType", ChangeType.Import, "imported");
        var notification = new uSyncImportCompletedNotification([action], "settings");
        var webhookEvent = new uSyncImportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"group\":\"settings\""));
            Assert.That(json, Does.Contain("\"success\":true"));
            Assert.That(json, Does.Contain("\"change\":\"Import\""));
            Assert.That(json, Does.Contain("\"exceptionMessage\":null"));
            Assert.That(json, Does.Not.Contain("\"exception\":"));
        });
    }

    [Test]
    public void ImportCompleted_NullActions_Serializes()
    {
        var notification = new uSyncImportCompletedNotification(null, "settings");
        var webhookEvent = new uSyncImportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        Assert.That(json, Does.Contain("\"actions\":[]"));
    }

    private string Serialize(object payload)
    {
        string json = null;
        Assert.DoesNotThrow(() => json = _serializer.Serialize(payload));
        return json;
    }

    private static XElement CreateItem(Guid key)
        => new("ContentType",
            new XAttribute("Key", key),
            new XAttribute("Alias", "home"),
            new XElement("Info"));

    private static uSyncAction CreateFailAction(Exception exception)
        => uSyncAction.Fail("bad", "handler", "type", ChangeType.Fail, "failed", exception);

    private static Exception GetThrownException()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void AssertItemJson(string json, Guid key, string change)
    {
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain($"\"key\":\"{key}\""));
            Assert.That(json, Does.Contain("\"alias\":\"home\""));
            Assert.That(json, Does.Contain("\"itemType\":\"ContentType\""));
            Assert.That(json, Does.Contain($"\"change\":\"{change}\""));
            Assert.That(json, Does.Not.Contain("firstAttribute"));
            Assert.That(json, Does.Not.Contain("handler"));
        });
    }

    private static void AssertCompletedJson(string json, string exceptionMessage)
    {
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"group\":\"settings\""));
            Assert.That(json, Does.Contain("\"actions\":["));
            Assert.That(json, Does.Contain($"\"exceptionMessage\":\"{exceptionMessage}\""));
            Assert.That(json, Does.Contain("\"handlerAlias\":\"handler\""));
            Assert.That(json, Does.Contain("\"success\":false"));
            Assert.That(json, Does.Contain("\"itemType\":\"type\""));
            Assert.That(json, Does.Contain("\"message\":\"failed\""));
            Assert.That(json, Does.Contain("\"change\":\"Fail\""));
            Assert.That(json, Does.Contain("\"name\":\"bad\""));
            Assert.That(json, Does.Contain("\"key\":"));
            Assert.That(json, Does.Not.Contain("targetSite"));
            Assert.That(json, Does.Not.Contain("\"exception\":"));
        });
    }
}
