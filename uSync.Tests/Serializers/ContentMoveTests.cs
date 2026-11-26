using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_15_0_0.LocalLinks;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  an import that moves an item and changes its values has to do both (#1300).
/// </summary>
/// <remarks>
///  setting Path and Level on the item made Umbraco treat the save as a bulk move
///  of a descendant, and skip saving the property values.
/// </remarks>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentMoveTests : UmbracoIntegrationTest
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

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IMediaService MediaService => GetRequiredService<IMediaService>();

    private ISyncSerializer<IContent> ContentSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>()!;

    private ISyncSerializer<IMedia> MediaSerializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IMedia>()!;

    private static SyncSerializerOptions Import => new(SerializerFlags.Force);

    private string CreateContentType()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();

        var contentType = new ContentType(shortStringHelper, -1) { Alias = "movePage", Name = "Move Page", AllowedAsRoot = true };
        contentType.AddPropertyGroup("content", "Content");
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox,
        }, "content");
        contentTypeService.Save(contentType);

        // allow the type under itself, now it has a key.
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
        contentTypeService.Save(contentType);
        return contentType.Alias;
    }

    private IContent CreatePage(string contentType, string name, int parentId, string title)
    {
        var content = ContentService.Create(name, parentId, contentType);
        content.SetValue("title", title);
        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);
        return ContentService.GetById(content.Key)!;
    }

    private static void SetTitle(XElement node, string title)
        => node.Element("Properties")!.Element("title")!.Element("Value")!.ReplaceAll(new XCData(title));

    private static void SetParent(XElement node, IEntity parent)
        => node.Element("Info")!.Element("Parent")!.SetAttributeValue("Key", parent.Key);

    [Test]
    public async Task Import_MoveAndPropertyChange_AppliesBoth()
    {
        var contentType = CreateContentType();
        var folderA = CreatePage(contentType, "Folder A", -1, "A");
        var folderB = CreatePage(contentType, "Folder B", folderA.Id, "B");
        var page = CreatePage(contentType, "Page", folderA.Id, "old");
        var child = CreatePage(contentType, "Child", page.Id, "child old");

        var pageNode = (await ContentSerializer.SerializeAsync(page, new SyncSerializerOptions())).Item!;
        var childNode = (await ContentSerializer.SerializeAsync(child, new SyncSerializerOptions())).Item!;

        SetParent(pageNode, folderB);
        SetTitle(pageNode, "new");
        SetTitle(childNode, "child new");

        var pageAttempt = await ContentSerializer.DeserializeAsync(pageNode, Import);
        Assert.That(pageAttempt.Success, Is.True, pageAttempt.Message);

        var childAttempt = await ContentSerializer.DeserializeAsync(childNode, Import);
        Assert.That(childAttempt.Success, Is.True, childAttempt.Message);

        var storedPage = ContentService.GetById(page.Key)!;
        Assert.That(storedPage.ParentId, Is.EqualTo(folderB.Id));
        Assert.That(storedPage.Path, Is.EqualTo($"{folderB.Path},{storedPage.Id}"));
        Assert.That(storedPage.Level, Is.EqualTo(folderB.Level + 1));
        Assert.That(storedPage.GetValue<string>("title"), Is.EqualTo("new"));
        Assert.That(storedPage.GetValue<string>("title", published: true), Is.EqualTo("new"));

        var storedChild = ContentService.GetById(child.Key)!;
        Assert.That(storedChild.Path, Is.EqualTo($"{storedPage.Path},{storedChild.Id}"));
        Assert.That(storedChild.Level, Is.EqualTo(storedPage.Level + 1));
        Assert.That(storedChild.GetValue<string>("title"), Is.EqualTo("child new"));
        Assert.That(storedChild.GetValue<string>("title", published: true), Is.EqualTo("child new"));
    }

    [Test]
    public async Task Preview_Move_DoesNotMoveStoredContent()
    {
        var contentType = CreateContentType();
        var folderA = CreatePage(contentType, "Folder A", -1, "A");
        var folderB = CreatePage(contentType, "Folder B", folderA.Id, "B");
        var page = CreatePage(contentType, "Page", folderA.Id, "old");

        var pageNode = (await ContentSerializer.SerializeAsync(page, new SyncSerializerOptions())).Item!;
        SetParent(pageNode, folderB);

        var attempt = await ContentSerializer.DeserializeAsync(pageNode, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.DoNotSave));

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.ParentId, Is.EqualTo(folderB.Id));
        Assert.That(attempt.Item.Path, Is.EqualTo($"{folderB.Path},{page.Id}"));
        Assert.That(ContentService.GetById(page.Key)!.ParentId, Is.EqualTo(folderA.Id));
    }

    [Test]
    public async Task Import_MediaMoveAndPropertyChange_AppliesBoth()
    {
        var mediaType = CreateMediaType();
        var folderA = CreateMedia(mediaType, "Media A", -1, "A");
        var folderB = CreateMedia(mediaType, "Media B", folderA.Id, "B");
        var item = CreateMedia(mediaType, "Media Item", folderA.Id, "old");

        var node = (await MediaSerializer.SerializeAsync(item, new SyncSerializerOptions())).Item!;
        SetParent(node, folderB);
        SetTitle(node, "new");

        var attempt = await MediaSerializer.DeserializeAsync(node, Import);
        Assert.That(attempt.Success, Is.True, attempt.Message);

        var stored = MediaService.GetById(item.Key)!;
        Assert.That(stored.ParentId, Is.EqualTo(folderB.Id));
        Assert.That(stored.Path, Is.EqualTo($"{folderB.Path},{stored.Id}"));
        Assert.That(stored.Level, Is.EqualTo(folderB.Level + 1));
        Assert.That(stored.GetValue<string>("title"), Is.EqualTo("new"));
    }

    private string CreateMediaType()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var mediaTypeService = GetRequiredService<IMediaTypeService>();

        var mediaType = new MediaType(shortStringHelper, -1) { Alias = "moveMedia", Name = "Move Media", AllowedAsRoot = true };
        mediaType.AddPropertyGroup("content", "Content");
        mediaType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox,
        }, "content");
        mediaTypeService.Save(mediaType);

        mediaType.AllowedContentTypes = [new ContentTypeSort(mediaType.Key, 0, mediaType.Alias)];
        mediaTypeService.Save(mediaType);
        return mediaType.Alias;
    }

    private IMedia CreateMedia(string mediaType, string name, int parentId, string title)
    {
        var media = MediaService.CreateMedia(name, parentId, mediaType);
        media.SetValue("title", title);
        MediaService.Save(media);
        return MediaService.GetById(media.Key)!;
    }
}
