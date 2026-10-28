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
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a preview runs the data type serializer with DoNotSave, it has to build the
///  incoming item in memory without writing anything (the item or its folders) to Umbraco.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DataTypePreviewTests : UmbracoIntegrationTest
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

    private IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();

    private IDataTypeContainerService ContainerService => GetRequiredService<IDataTypeContainerService>();

    private ISyncSerializer<IDataType> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IDataType>()!;

    private static SyncSerializerOptions DryRun => new(SerializerFlags.Force | SerializerFlags.DoNotSave);

    private async Task<IDataType> CreateTextBoxAsync(string name, int maxChars)
    {
        var editor = GetRequiredService<PropertyEditorCollection>()[UmbConstants.PropertyEditors.Aliases.TextBox];
        var dataType = new DataType(editor!, GetRequiredService<IConfigurationEditorJsonSerializer>())
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
            ConfigurationData = new Dictionary<string, object> { ["maxChars"] = maxChars },
        };

        var attempt = await DataTypeService.CreateAsync(dataType, UmbConstants.Security.SuperUserKey);
        Assert.That(attempt.Success, Is.True);
        return attempt.Result;
    }

    private static XElement NewTextBoxNode(Guid key, string name, string folder)
        => new("DataType",
            new XAttribute("Key", key),
            new XAttribute("Alias", name),
            new XAttribute("Level", 2),
            new XElement("Info",
                new XElement("Name", name),
                new XElement("EditorAlias", UmbConstants.PropertyEditors.Aliases.TextBox),
                new XElement("EditorUIAlias", "Umb.PropertyEditorUi.TextBox"),
                new XElement("Folder", folder)),
            new XElement("Config", new XCData("{ \"maxChars\": 42 }")));

    private async Task<int> FolderCountAsync(string name)
        => (await ContainerService.GetAsync(name, 1)).Count();

    [Test]
    public async Task DoNotSave_ExistingDataType_ChangesInMemoryOnly()
    {
        var dataType = await CreateTextBoxAsync("Preview TextBox", 10);

        var node = (await Serializer.SerializeAsync(dataType, new SyncSerializerOptions())).Item!;
        node.Element("Info")!.Element("Name")!.Value = "Preview TextBox (renamed)";
        node.Element("Config")!.ReplaceAll(new XCData("{ \"maxChars\": 99 }"));

        var attempt = await Serializer.DeserializeAsync(node, DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Name, Is.EqualTo("Preview TextBox (renamed)"));
        Assert.That(attempt.Item.ConfigurationData["maxChars"]?.ToString(), Is.EqualTo("99"));

        var stored = await DataTypeService.GetAsync(dataType.Key);
        Assert.That(stored!.Name, Is.EqualTo("Preview TextBox"));
        Assert.That(stored.ConfigurationData["maxChars"]?.ToString(), Is.EqualTo("10"));
    }

    [Test]
    public async Task DoNotSave_NewDataType_DoesNotCreateItemOrFolder()
    {
        var key = Guid.NewGuid();
        var attempt = await Serializer.DeserializeAsync(NewTextBoxNode(key, "Preview New", "Preview Folder"), DryRun);

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(attempt.Item!.Key, Is.EqualTo(key));
        Assert.That(attempt.Item.HasIdentity, Is.False);

        Assert.That(await DataTypeService.GetAsync(key), Is.Null);
        Assert.That(await FolderCountAsync("Preview Folder"), Is.Zero);
    }

    [Test]
    public async Task Import_NewDataType_StillCreatesFolder()
    {
        var key = Guid.NewGuid();
        var attempt = await Serializer.DeserializeAsync(
            NewTextBoxNode(key, "Imported New", "Import Folder"),
            new SyncSerializerOptions(SerializerFlags.Force));

        Assert.That(attempt.Success, Is.True, attempt.Message);
        Assert.That(await DataTypeService.GetAsync(key), Is.Not.Null);
        Assert.That(await FolderCountAsync("Import Folder"), Is.EqualTo(1));
    }

    [Test]
    public async Task DataTypeHandler_Preview_ReturnsCurrentAndIncoming()
    {
        var dataType = await CreateTextBoxAsync("Handler Preview", 10);
        var node = (await Serializer.SerializeAsync(dataType, new SyncSerializerOptions())).Item!;
        node.Element("Config")!.ReplaceAll(new XCData("{ \"maxChars\": 25 }"));

        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.DataTypeHandler);
        Assert.That(handler, Is.Not.Null);

        var preview = await handler!.Handler.GetPreviewAsync(node, handler.Settings);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.Success, Is.True, preview.Message);
        Assert.That(((IDataType)preview.Current!).ConfigurationData["maxChars"]?.ToString(), Is.EqualTo("10"));
        Assert.That(((IDataType)preview.Incoming!).ConfigurationData["maxChars"]?.ToString(), Is.EqualTo("25"));

        var stored = await DataTypeService.GetAsync(dataType.Key);
        Assert.That(stored!.ConfigurationData["maxChars"]?.ToString(), Is.EqualTo("10"));
    }

    [Test]
    public async Task Handler_WithoutPreviewSupport_ReturnsNull()
    {
        var handler = GetRequiredService<ISyncHandlerFactory>().GetValidHandler(uSync.BackOffice.uSyncConstants.Handlers.ContentTypeHandler);
        Assert.That(handler, Is.Not.Null);

        var preview = await handler!.Handler.GetPreviewAsync(new XElement("ContentType"), handler.Settings);
        Assert.That(preview, Is.Null);
    }
}
