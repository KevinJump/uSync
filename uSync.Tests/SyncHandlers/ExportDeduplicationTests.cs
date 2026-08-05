using System;
using System.Collections.Generic;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers.Handlers;

namespace uSync.Tests.SyncHandlers;

/// <summary>
///  Tests for the export de-duplication used by the publishable content handlers.
/// </summary>
/// <remarks>
///  From Umbraco 18.1 a save-and-publish raises the saved notification as well as the published
///  one (umbraco/Umbraco-CMS#23523). Both notifications share a single notification state object,
///  which is what lets us export the item only once per operation.
/// </remarks>
[TestFixture]
public class ExportDeduplicationTests
{
    private static IContent MakeContent(Guid key)
    {
        var content = new Mock<IContent>();
        content.SetupGet(x => x.Key).Returns(key);
        content.SetupGet(x => x.Name).Returns($"item-{key}");
        return content.Object;
    }

    /// <summary>
    ///  build the pair of notifications Umbraco raises for a save-and-publish, sharing one state
    ///  object the way ContentService does via .WithState(notificationState).
    /// </summary>
    private static (ContentSavedNotification Saved, ContentPublishedNotification Published) SaveAndPublishNotifications(IContent item)
    {
        var messages = new EventMessages();
        var state = new Dictionary<string, object>();

        var saved = new ContentSavedNotification(item, messages) { State = state };
        var published = new ContentPublishedNotification(item, messages) { State = state };

        return (saved, published);
    }

    [Test]
    public void Item_Is_Only_Claimed_Once_Across_A_Save_And_Publish()
    {
        var item = MakeContent(Guid.NewGuid());
        var (saved, published) = SaveAndPublishNotifications(item);

        Assert.Multiple(() =>
        {
            Assert.That(
                PublishableContentHandlerBase<IContent>.ClaimItemForExport(saved, item), Is.True,
                "the saved notification arrives first, so it should export the item");

            Assert.That(
                PublishableContentHandlerBase<IContent>.ClaimItemForExport(published, item), Is.False,
                "the published notification shares the state, so it should not export the item again");
        });
    }

    [Test]
    public void Unpublished_Notification_Is_Also_De_Duplicated()
    {
        // un-publishing a single culture is performed as a publish, and raises the saved
        // notification alongside it.
        var item = MakeContent(Guid.NewGuid());
        var messages = new EventMessages();
        var state = new Dictionary<string, object>();

        var saved = new ContentSavedNotification(item, messages) { State = state };
        var unpublished = new ContentUnpublishedNotification(item, messages) { State = state };

        Assert.Multiple(() =>
        {
            Assert.That(PublishableContentHandlerBase<IContent>.ClaimItemForExport(saved, item), Is.True);
            Assert.That(PublishableContentHandlerBase<IContent>.ClaimItemForExport(unpublished, item), Is.False);
        });
    }

    [Test]
    public void Publish_Without_A_Save_Still_Claims_The_Item()
    {
        // publishing from the tree raises only the published notification - it must still export.
        var item = MakeContent(Guid.NewGuid());
        var (_, published) = SaveAndPublishNotifications(item);

        Assert.That(PublishableContentHandlerBase<IContent>.ClaimItemForExport(published, item), Is.True);
    }

    [Test]
    public void Each_Item_In_A_Branch_Publish_Is_Claimed()
    {
        // one notification, many documents, a single shared state - every document exports once.
        var items = new[] { MakeContent(Guid.NewGuid()), MakeContent(Guid.NewGuid()), MakeContent(Guid.NewGuid()) };
        var published = new ContentPublishedNotification(items, new EventMessages(), true)
        {
            State = new Dictionary<string, object>()
        };

        Assert.Multiple(() =>
        {
            foreach (var item in items)
            {
                Assert.That(
                    PublishableContentHandlerBase<IContent>.ClaimItemForExport(published, item), Is.True,
                    $"{item.Name} should be exported");

                Assert.That(
                    PublishableContentHandlerBase<IContent>.ClaimItemForExport(published, item), Is.False,
                    $"{item.Name} should not be exported twice");
            }
        });
    }

    [Test]
    public void A_Later_Operation_Claims_The_Item_Again()
    {
        // the tracking is per-operation, a second save of the same item must export it again.
        var item = MakeContent(Guid.NewGuid());

        var (firstSaved, _) = SaveAndPublishNotifications(item);
        var (secondSaved, _) = SaveAndPublishNotifications(item);

        Assert.Multiple(() =>
        {
            Assert.That(PublishableContentHandlerBase<IContent>.ClaimItemForExport(firstSaved, item), Is.True);
            Assert.That(PublishableContentHandlerBase<IContent>.ClaimItemForExport(secondSaved, item), Is.True);
        });
    }

    [Test]
    public void Existing_Notification_State_Is_Preserved()
    {
        // uSync writes its own keys into the notification state, we must not stamp on them.
        var item = MakeContent(Guid.NewGuid());
        var (saved, published) = SaveAndPublishNotifications(item);

        saved.State[uSync.BackOffice.uSync.EventStateKey] = true;

        PublishableContentHandlerBase<IContent>.ClaimItemForExport(saved, item);

        Assert.That(published.State[uSync.BackOffice.uSync.EventStateKey], Is.True);
    }
}
