using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Serialization;

using CoreConstants = uSync.Core.uSyncConstants;

namespace uSync.Tests.Handlers;

/// <summary>
///  the MoveToRecycleBin and ExportTrashedAsDelete settings, so an item in the recycle bin on
///  one site can be sent as a delete, and end up in the recycle bin on the other.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class TrashedItemDeleteTests : UmbracoIntegrationTest
{
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "1",
            ["Tests:Database:SchemaDatabaseCount"] = "1",
            ["Tests:Database:EmptyDatabasesCount"] = "0"
        });
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AdduSync(settings =>
        {
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "None";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    private const string _root = "uSync/tests/trashed";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private IContentService ContentService => GetRequiredService<IContentService>();
    private IMediaService MediaService => GetRequiredService<IMediaService>();

    private ISyncSerializer<IContent> ContentSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>()!;

    private ISyncSerializer<IMedia> MediaSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IMedia>()!;

    private ContentHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();

    private async Task<IContent> CreateContent()
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var content = ContentService.Create("home", -1, contentType);
        ContentService.Save(content);
        return content;
    }

    private async Task<IMedia> CreateMedia()
    {
        var mediaType = new MediaType(GetRequiredService<IShortStringHelper>(), -1) { Alias = "photo", Name = "Photo", AllowedAsRoot = true };
        await GetRequiredService<IMediaTypeService>().CreateAsync(mediaType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var media = MediaService.CreateMedia("photo", -1, mediaType.Alias);
        MediaService.Save(media);
        return media;
    }

    private static XElement DeleteNode(Guid key, string itemType, string? recycleBin = null)
    {
        var node = XElementExtensions.MakeEmpty(key, SyncActionType.Delete, "item");
        node.Add(new XAttribute(CoreConstants.Xml.ItemType, itemType));
        if (recycleBin is not null)
            node.Add(new XAttribute(CoreConstants.Xml.RecycleBin, recycleBin));
        return node;
    }

    private static SyncSerializerOptions Options(bool moveToRecycleBin)
        => new(new Dictionary<string, object?>
        {
            // appsettings.json values bind as strings.
            [CoreConstants.DefaultSettings.MoveToRecycleBin] = moveToRecycleBin.ToString()
        });

    [Test]
    public async Task ContentDelete_WithMoveToRecycleBin_GoesToTheBin()
    {
        var content = await CreateContent();

        var attempt = await ContentSerializer.DeserializeAsync(DeleteNode(content.Key, "Content"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(ContentService.GetById(content.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task MediaDelete_WithMoveToRecycleBin_GoesToTheBin()
    {
        var media = await CreateMedia();

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(MediaService.GetById(media.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task MediaDelete_ByDefault_DeletesOutright()
    {
        var media = await CreateMedia();

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(false));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(MediaService.GetById(media.Key), Is.Null);
    }

    [Test]
    public async Task Delete_OfAnItemAlreadyInTheBin_IsNoChange()
    {
        var media = await CreateMedia();
        MediaService.MoveToRecycleBin(media);

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.NoChange));
        Assert.That(MediaService.GetById(media.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task DeleteFromTheBin_WithMoveToRecycleBin_DeletesOutright()
    {
        var media = await CreateMedia();
        MediaService.MoveToRecycleBin(media);

        var node = DeleteNode(media.Key, "Media", CoreConstants.Xml.RecycleBinDelete);
        var attempt = await MediaSerializer.DeserializeAsync(node, Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(MediaService.GetById(media.Key), Is.Null);
    }

    [Test]
    public async Task MoveDelete_OfAnItemAlreadyInTheBin_IsNoChange()
    {
        var content = await CreateContent();
        ContentService.MoveToRecycleBin(content);

        var node = DeleteNode(content.Key, "Content", CoreConstants.Xml.RecycleBinMove);
        var attempt = await ContentSerializer.DeserializeAsync(node, Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.NoChange));
        Assert.That(ContentService.GetById(content.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task SerializeEmpty_DeleteOfABinnedItem_IsMarkedAsFromTheBin()
    {
        var content = await CreateContent();
        ContentService.MoveToRecycleBin(content);
        content = ContentService.GetById(content.Key)!;

        var trashed = await ContentSerializer.SerializeEmptyAsync(content, SyncActionType.Delete, string.Empty);
        var other = ContentService.Create("other", -1, content.ContentType.Alias);
        ContentService.Save(other);
        var live = await ContentSerializer.SerializeEmptyAsync(other, SyncActionType.Delete, string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(trashed.Item?.Attribute(CoreConstants.Xml.RecycleBin)?.Value, Is.EqualTo(CoreConstants.Xml.RecycleBinDelete));
            Assert.That(trashed.Item?.GetItemType(), Is.EqualTo("Content"));
            Assert.That(live.Item?.Attribute(CoreConstants.Xml.RecycleBin), Is.Null);
        });
    }

    [Test]
    public async Task Export_TrashedContent_WithExportTrashedAsDelete_IsADelete()
    {
        var content = await CreateContent();
        ContentService.MoveToRecycleBin(content);
        content = ContentService.GetById(content.Key)!;

        var config = Handler.DefaultConfig.Clone();
        config.Settings[CoreConstants.DefaultSettings.ExportTrashedAsDelete] = "true";

        var actions = (await Handler.ExportAsync(content, [$"{_root}/{Guid.NewGuid():N}/"], config)).ToList();
        var file = actions.Single(x => x.Success).FileName!;
        var node = await GetRequiredService<ISyncFileService>().LoadXElementAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(node.IsEmptyItem(), Is.True);
            Assert.That(node.GetEmptyAction(), Is.EqualTo(SyncActionType.Delete));
            Assert.That(node.GetKey(), Is.EqualTo(content.Key));
            Assert.That(node.GetItemType(), Is.EqualTo("Content"));
            Assert.That(node.Attribute(CoreConstants.Xml.RecycleBin)?.Value, Is.EqualTo(CoreConstants.Xml.RecycleBinMove));
        });
    }

    [Test]
    public async Task Export_TrashedContent_WithExportTrashedOff_WritesNoDelete()
    {
        var content = await CreateContent();
        ContentService.MoveToRecycleBin(content);
        content = ContentService.GetById(content.Key)!;

        var config = Handler.DefaultConfig.Clone();
        config.Settings[CoreConstants.DefaultSettings.ExportTrashedAsDelete] = "true";
        config.Settings["ExportTrashed"] = "false";

        var actions = (await Handler.ExportAsync(content, [$"{_root}/{Guid.NewGuid():N}/"], config)).ToList();

        Assert.That(actions.Any(x => x.Change > ChangeType.NoChange), Is.False);
        Assert.That(actions.Where(x => x.FileName is not null)
            .Any(x => GetRequiredService<ISyncFileService>().FileExists(x.FileName!)), Is.False);
    }

    [Test]
    public async Task BlueprintDelete_WithMoveToRecycleBin_DeletesOutright()
    {
        var content = await CreateContent();
        var blueprint = ContentService.CreateBlueprintFromContent(content, "blueprint");
        ContentService.SaveBlueprint(blueprint, null);

        var serializer = GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("contentTemplateSerializer");
        Assert.That(serializer, Is.Not.Null);

        var attempt = await serializer!.DeserializeAsync(DeleteNode(blueprint.Key, "Content"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(ContentService.GetBlueprintById(blueprint.Key), Is.Null);
    }

    [Test]
    public async Task Export_TrashedContent_ByDefault_IsTheItem()
    {
        var content = await CreateContent();
        ContentService.MoveToRecycleBin(content);
        content = ContentService.GetById(content.Key)!;

        var actions = (await Handler.ExportAsync(content, [$"{_root}/{Guid.NewGuid():N}/"], Handler.DefaultConfig)).ToList();
        var node = await GetRequiredService<ISyncFileService>().LoadXElementAsync(actions.Single(x => x.Success).FileName!);

        Assert.That(node.IsEmptyItem(), Is.False);
    }
}
