using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a content type preview runs the serializer (both passes) with DoNotSave, it has to build
///  the incoming type in memory without writing the type or its folders to Umbraco.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypePreviewTests : UmbracoIntegrationTest
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

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private ISyncSerializer<IContentType> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContentType>()!;

    private static SyncSerializerOptions DryRun
        => new(SerializerFlags.Force | SerializerFlags.OnePass | SerializerFlags.DoNotSave);

    private IContentType CreateContentType(string alias)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = alias, Name = alias };
        contentType.AddPropertyGroup("content", "Content");
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox,
        }, "content");
        ContentTypeService.Save(contentType);
        return ContentTypeService.Get(contentType.Key)!;
    }

    private async Task<XElement> SerializeAsync(IContentType contentType)
        => (await Serializer.SerializeAsync(contentType, new SyncSerializerOptions())).Item!;

    private async Task<int> FolderCountAsync(string name)
        => (await GetRequiredService<IContentTypeContainerService>().GetAsync(name, 1)).Count();

    [Test]
    public async Task DoNotSave_ExistingContentType_ChangesInMemoryOnly()
    {
        var contentType = CreateContentType("previewPage");

        var node = await SerializeAsync(contentType);
        node.Element("Info")!.Element("Name")!.Value = "Preview Page (renamed)";
        node.Element("GenericProperties")!.Elements("GenericProperty").First().Element("Name")!.Value = "Heading";

        var attempt = await Serializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Name, Is.EqualTo("Preview Page (renamed)"));
        Assert.That(attempt.Item.PropertyTypes.Single().Name, Is.EqualTo("Heading"));

        var stored = ContentTypeService.Get(contentType.Key)!;
        Assert.That(stored.Name, Is.EqualTo("previewPage"));
        Assert.That(stored.PropertyTypes.Single().Name, Is.EqualTo("Title"));
    }

    [Test]
    public async Task DoNotSave_NewContentType_DoesNotCreateItemOrFolder()
    {
        var existing = CreateContentType("template");
        var node = await SerializeAsync(existing);

        // turn it into a brand new type in a folder that doesn't exist.
        var key = Guid.NewGuid();
        node.SetAttributeValue("Key", key);
        node.SetAttributeValue("Alias", "brandNew");
        node.Element("Info")!.Element("Name")!.Value = "Brand New";
        node.Element("Info")!.Add(new XElement("Folder", "Preview Folder"));
        foreach (var property in node.Element("GenericProperties")!.Elements("GenericProperty"))
            property.Element("Key")!.Value = Guid.NewGuid().ToString();
        foreach (var tab in node.Element("Tabs")!.Elements("Tab"))
            tab.Element("Key")!.Value = Guid.NewGuid().ToString();

        var attempt = await Serializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Key, Is.EqualTo(key));
        Assert.That(attempt.Item.HasIdentity, Is.False);

        Assert.That(ContentTypeService.Get(key), Is.Null);
        Assert.That(await FolderCountAsync("Preview Folder"), Is.Zero);
    }

    [Test]
    public async Task ContentTypeHandler_Preview_ReturnsCurrentAndIncoming()
    {
        var contentType = CreateContentType("handlerPreview");
        var node = await SerializeAsync(contentType);
        node.Element("Info")!.Element("Name")!.Value = "Handler Preview (incoming)";

        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.ContentTypeHandler);
        Assert.That(handler, Is.Not.Null);

        var preview = await handler!.Handler.GetPreviewAsync(node, handler.Settings);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.Success, Is.True, preview.Message);
        Assert.That(((IContentType)preview.Current!).Name, Is.EqualTo("handlerPreview"));
        Assert.That(((IContentType)preview.Incoming!).Name, Is.EqualTo("Handler Preview (incoming)"));

        Assert.That(ContentTypeService.Get(contentType.Key)!.Name, Is.EqualTo("handlerPreview"));
    }

    /// <summary>
    ///  a property can use a data type that only exists in the uSync files (the import creates it first).
    /// </summary>
    [Test]
    public async Task Preview_NewPropertyWithPendingDataType_KeepsProperty()
    {
        var contentType = CreateContentType("pendingDataType");
        var node = await SerializeAsync(contentType);

        var pendingKey = Guid.NewGuid();
        var editor = GetRequiredService<PropertyEditorCollection>()[UmbConstants.PropertyEditors.Aliases.TextBox];
        var pending = new DataType(editor!, GetRequiredService<IConfigurationEditorJsonSerializer>()) { Key = pendingKey, Name = "Pending" };

        var property = new XElement(node.Element("GenericProperties")!.Elements("GenericProperty").First());
        property.Element("Key")!.Value = Guid.NewGuid().ToString();
        property.Element("Alias")!.Value = "subtitle";
        property.Element("Name")!.Value = "Subtitle";
        property.Element("Definition")!.Value = pendingKey.ToString();
        node.Element("GenericProperties")!.Add(property);

        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.ContentTypeHandler)!;

        var without = await handler.Handler.GetPreviewAsync(node, handler.Settings);
        Assert.That(((IContentType)without!.Incoming!).PropertyTypes.Any(x => x.Alias == "subtitle"), Is.False);

        var options = new SyncPreviewOptions
        {
            PendingItemResolver = (type, key) => Task.FromResult<object?>(type == typeof(IDataType) && key == pendingKey ? pending : null),
        };

        var with = await handler.Handler.GetPreviewAsync(node, handler.Settings, options);
        var subtitle = ((IContentType)with!.Incoming!).PropertyTypes.SingleOrDefault(x => x.Alias == "subtitle");
        Assert.That(subtitle, Is.Not.Null);
        Assert.That(subtitle!.DataTypeKey, Is.EqualTo(pendingKey));

        Assert.That(ContentTypeService.Get(contentType.Key)!.PropertyTypes.Any(x => x.Alias == "subtitle"), Is.False);
    }
}
