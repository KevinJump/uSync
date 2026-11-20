using System;
using System.Linq;
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
    public void ImportCompleted_WithNeverThrownException_Serializes()
    {
        var notification = new uSyncImportCompletedNotification(
            [CreateFailAction(new Exception("not thrown"))], "settings");
        var webhookEvent = new uSyncImportCompletedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertCompletedJson(json, "not thrown");
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
            Assert.That(json, Does.Contain("\"exception\":null"));
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

    [Test]
    public void ImportCompleted_IncludesSummaryAndFailures()
    {
        var notification = new uSyncImportCompletedNotification(
        [
            uSyncAction.SetAction(true, "a", "ContentType", ChangeType.Create, "created"),
            uSyncAction.SetAction(true, "b", "ContentType", ChangeType.Import, "imported"),
            uSyncAction.SetAction(true, "c", "ContentType", ChangeType.NoChange, "same"),
            uSyncAction.SetAction(true, "d", "ContentType", ChangeType.Delete, "deleted"),
            CreateFailAction(new Exception("bad one")),
        ], "settings");

        var payload = (uSyncBulkWebhookPayload)CreateImportCompleted().ConvertNotificationToRequestPayload(notification);

        Assert.Multiple(() =>
        {
            Assert.That(payload.Server, Is.EqualTo(Environment.MachineName));
            Assert.That(payload.Summary.Total, Is.EqualTo(5));
            Assert.That(payload.Summary.Created, Is.EqualTo(1));
            Assert.That(payload.Summary.Updated, Is.EqualTo(1));
            Assert.That(payload.Summary.Deleted, Is.EqualTo(1));
            Assert.That(payload.Summary.NoChange, Is.EqualTo(1));
            Assert.That(payload.Summary.Failed, Is.EqualTo(1));
            Assert.That(payload.Failures, Has.Count.EqualTo(1));
            Assert.That(payload.Failures[0].Name, Is.EqualTo("bad"));
            Assert.That(payload.Actions, Has.Count.EqualTo(5));
        });

        var json = Serialize(payload);
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"summary\":{\"total\":5"));
            Assert.That(json, Does.Contain("\"failures\":[{"));
            Assert.That(json, Does.Contain("\"server\":"));
        });
    }

    [Test]
    public void ImportCompleted_CapsFailures()
    {
        var actions = Enumerable.Range(0, uSyncBulkWebhookPayload.MaxFailures + 10)
            .Select(_ => CreateFailAction(new Exception("x")))
            .ToList();

        var payload = (uSyncBulkWebhookPayload)CreateImportCompleted()
            .ConvertNotificationToRequestPayload(new uSyncImportCompletedNotification(actions, "settings"));

        Assert.Multiple(() =>
        {
            Assert.That(payload.Failures, Has.Count.EqualTo(uSyncBulkWebhookPayload.MaxFailures));
            Assert.That(payload.Summary.Failed, Is.EqualTo(uSyncBulkWebhookPayload.MaxFailures + 10));
        });
    }

    [Test]
    public void ImportFailed_FiresOnlyWhenImportHasErrors()
    {
        var webhookEvent = new uSyncImportFailedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);
        var ok = uSyncAction.SetAction(true, "home", "ContentType", ChangeType.Import, "imported");

        Assert.Multiple(() =>
        {
            Assert.That(webhookEvent.Alias, Is.EqualTo("uSyncImportFailed"));
            Assert.That(webhookEvent.ShouldFireWebhookForNotification(
                new uSyncImportCompletedNotification([ok], "settings")), Is.False);
            Assert.That(webhookEvent.ShouldFireWebhookForNotification(
                new uSyncImportCompletedNotification(null, "settings")), Is.False);
            Assert.That(webhookEvent.ShouldFireWebhookForNotification(
                new uSyncImportCompletedNotification([ok, CreateFailAction(new Exception("x"))], "settings")), Is.True);
        });
    }

    [Test]
    public void ImportFailed_Serializes()
    {
        var notification = new uSyncImportCompletedNotification([CreateFailAction(GetThrownException())], "settings");
        var webhookEvent = new uSyncImportFailedWebhookEvent(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

        var json = Serialize(webhookEvent.ConvertNotificationToRequestPayload(notification));

        AssertCompletedJson(json, "boom");
    }

    private uSyncImportCompletedWebhookEvent CreateImportCompleted()
        => new(_firingService, _webhookService, _webhookSettings, _serverRoleAccessor);

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
            Assert.That(json, Does.Contain($"\"exception\":{{\"message\":\"{exceptionMessage}\"}}"));
            Assert.That(json, Does.Not.Contain("targetSite"));
            Assert.That(json, Does.Not.Contain("stackTrace"));
            Assert.That(json, Does.Not.Contain("innerException"));
        });
    }
}
