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
using Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_15_0_0.LocalLinks;
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
        new ConvertLocalLinkComposer().Compose(builder);

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

    private IContent CreateContent()
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        GetRequiredService<IContentTypeService>().Save(contentType);

        var content = ContentService.Create("home", -1, contentType);
        ContentService.Save(content);
        return content;
    }

    private IMedia CreateMedia()
    {
        var mediaType = new MediaType(GetRequiredService<IShortStringHelper>(), -1) { Alias = "photo", Name = "Photo", AllowedAsRoot = true };
        GetRequiredService<IMediaTypeService>().Save(mediaType);

        var media = MediaService.CreateMedia("photo", -1, mediaType.Alias);
        MediaService.Save(media);
        return media;
    }

    private static XElement DeleteNode(Guid key, string itemType)
    {
        var node = XElementExtensions.MakeEmpty(key, SyncActionType.Delete, "item");
        node.Add(new XAttribute(CoreConstants.Xml.ItemType, itemType));
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
        var content = CreateContent();

        var attempt = await ContentSerializer.DeserializeAsync(DeleteNode(content.Key, "Content"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(ContentService.GetById(content.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task MediaDelete_WithMoveToRecycleBin_GoesToTheBin()
    {
        var media = CreateMedia();

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(MediaService.GetById(media.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task MediaDelete_ByDefault_DeletesOutright()
    {
        var media = CreateMedia();

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(false));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.Delete));
        Assert.That(MediaService.GetById(media.Key), Is.Null);
    }

    [Test]
    public async Task Delete_OfAnItemAlreadyInTheBin_IsNoChange()
    {
        var media = CreateMedia();
        MediaService.MoveToRecycleBin(media);

        var attempt = await MediaSerializer.DeserializeAsync(DeleteNode(media.Key, "Media"), Options(true));

        Assert.That(attempt.Change, Is.EqualTo(ChangeType.NoChange));
        Assert.That(MediaService.GetById(media.Key)?.Trashed, Is.True);
    }

    [Test]
    public async Task Export_TrashedContent_WithExportTrashedAsDelete_IsADelete()
    {
        var content = CreateContent();
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
        });
    }

    [Test]
    public async Task Export_TrashedContent_ByDefault_IsTheItem()
    {
        var content = CreateContent();
        ContentService.MoveToRecycleBin(content);
        content = ContentService.GetById(content.Key)!;

        var actions = (await Handler.ExportAsync(content, [$"{_root}/{Guid.NewGuid():N}/"], Handler.DefaultConfig)).ToList();
        var node = await GetRequiredService<ISyncFileService>().LoadXElementAsync(actions.Single(x => x.Success).FileName!);

        Assert.That(node.IsEmptyItem(), Is.False);
    }
}
