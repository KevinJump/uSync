using System;
using System.Collections.Generic;
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
using uSync.BackOffice.SyncHandlers;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a preview runs the content and media serializers with DoNotSave, they have to build
///  the incoming item in memory without saving or publishing anything.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentPreviewTests : UmbracoIntegrationTest
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
        // the RTE mapper needs the local link processor, which a full site registers.
        new ConvertLocalLinkComposer().Compose(builder);

        builder.AdduSync(settings =>
        {
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "None";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IMediaService MediaService => GetRequiredService<IMediaService>();

    private ISyncSerializer<IContent> ContentSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>()!;

    private ISyncSerializer<IMedia> MediaSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IMedia>()!;

    private static SyncSerializerOptions DryRun => new(SerializerFlags.Force | SerializerFlags.DoNotSave);

    private IContent CreatePage(string name, string title)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();

        var contentType = new ContentType(shortStringHelper, -1) { Alias = "previewPage", Name = "Preview Page", AllowedAsRoot = true };
        contentType.AddPropertyGroup("content", "Content");
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox,
        }, "content");
        contentTypeService.Save(contentType);

        var content = ContentService.Create(name, -1, contentType.Alias);
        content.SetValue("title", title);
        ContentService.Save(content);
        return ContentService.GetById(content.Key)!;
    }

    private IMedia CreateFolder(string name)
    {
        var media = MediaService.CreateMedia(name, -1, UmbConstants.Conventions.MediaTypes.Folder);
        MediaService.Save(media);
        return MediaService.GetById(media.Key)!;
    }

    private static void SetTitle(XElement node, string title)
        => node.Element("Properties")!.Element("title")!.Element("Value")!.ReplaceAll(new XCData(title));

    private static void SetName(XElement node, string name)
    {
        node.SetAttributeValue("Alias", name);
        node.Element("Info")!.Element("NodeName")!.SetAttributeValue("Default", name);
    }

    [Test]
    public async Task DoNotSave_ExistingContent_ChangesInMemoryOnly()
    {
        var content = CreatePage("Preview Home", "Old title");
        var versions = ContentService.GetVersionIds(content.Id, 10);

        var node = (await ContentSerializer.SerializeAsync(content, new SyncSerializerOptions())).Item!;
        SetName(node, "Preview Home (renamed)");
        SetTitle(node, "New title");

        var attempt = await ContentSerializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Name, Is.EqualTo("Preview Home (renamed)"));
        Assert.That(attempt.Item.GetValue<string>("title"), Is.EqualTo("New title"));

        var stored = ContentService.GetById(content.Key)!;
        Assert.That(stored.Name, Is.EqualTo("Preview Home"));
        Assert.That(stored.GetValue<string>("title"), Is.EqualTo("Old title"));
        Assert.That(stored.Published, Is.False);
        Assert.That(ContentService.GetVersionIds(content.Id, 10), Is.EquivalentTo(versions));
    }

    [Test]
    public async Task DoNotSave_NewContent_IsNotCreated()
    {
        var content = CreatePage("Preview Existing", "Title");
        var node = (await ContentSerializer.SerializeAsync(content, new SyncSerializerOptions())).Item!;

        var key = Guid.NewGuid();
        node.SetAttributeValue("Key", key);
        SetName(node, "Preview New Page");

        var attempt = await ContentSerializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Key, Is.EqualTo(key));
        Assert.That(attempt.Item.HasIdentity, Is.False);
        Assert.That(ContentService.GetById(key), Is.Null);
    }

    [Test]
    public async Task ContentHandler_Preview_ReturnsCurrentAndIncoming()
    {
        var content = CreatePage("Handler Home", "Old title");
        var node = (await ContentSerializer.SerializeAsync(content, new SyncSerializerOptions())).Item!;
        SetTitle(node, "New title");

        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.ContentHandler);
        Assert.That(handler, Is.Not.Null);

        var preview = await handler!.Handler.GetPreviewAsync(node, handler.Settings);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.Success, Is.True, preview.Message);
        Assert.That(preview.Node, Is.SameAs(node));
        Assert.That(preview.Options, Is.Not.Null);
        Assert.That(((IContent)preview.Current!).GetValue<string>("title"), Is.EqualTo("Old title"));
        Assert.That(((IContent)preview.Incoming!).GetValue<string>("title"), Is.EqualTo("New title"));

        Assert.That(ContentService.GetById(content.Key)!.GetValue<string>("title"), Is.EqualTo("Old title"));
    }

    [Test]
    public async Task DoNotSave_ExistingMedia_ChangesInMemoryOnly()
    {
        var media = CreateFolder("Preview Folder");
        var node = (await MediaSerializer.SerializeAsync(media, new SyncSerializerOptions())).Item!;
        SetName(node, "Preview Folder (renamed)");

        var attempt = await MediaSerializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Name, Is.EqualTo("Preview Folder (renamed)"));
        Assert.That(MediaService.GetById(media.Key)!.Name, Is.EqualTo("Preview Folder"));
    }

    [Test]
    public async Task DoNotSave_NewMedia_IsNotCreated()
    {
        var media = CreateFolder("Preview Existing Folder");
        var node = (await MediaSerializer.SerializeAsync(media, new SyncSerializerOptions())).Item!;

        var key = Guid.NewGuid();
        node.SetAttributeValue("Key", key);
        SetName(node, "Preview New Folder");

        var attempt = await MediaSerializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.HasIdentity, Is.False);
        Assert.That(MediaService.GetById(key), Is.Null);
    }

    [Test]
    public async Task MediaHandler_Preview_ReturnsCurrentAndIncoming()
    {
        var media = CreateFolder("Handler Folder");
        var node = (await MediaSerializer.SerializeAsync(media, new SyncSerializerOptions())).Item!;
        SetName(node, "Handler Folder (renamed)");

        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.MediaHandler);
        Assert.That(handler, Is.Not.Null);

        var preview = await handler!.Handler.GetPreviewAsync(node, handler.Settings);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.Success, Is.True, preview.Message);
        Assert.That(((IMedia)preview.Current!).Name, Is.EqualTo("Handler Folder"));
        Assert.That(((IMedia)preview.Incoming!).Name, Is.EqualTo("Handler Folder (renamed)"));
        Assert.That(MediaService.GetById(media.Key)!.Name, Is.EqualTo("Handler Folder"));
    }
}
