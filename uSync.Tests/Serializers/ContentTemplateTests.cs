using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  blueprint (content template) import, Umbraco 19 only allows blueprints for
///  some content types.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTemplateTests : UmbracoIntegrationTest
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

    private ISyncSerializer<IContent> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("contentTemplateSerializer");

    private async Task<IContentType> CreateContentType(string alias, bool isElement)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = alias,
            Name = alias,
            IsElement = isElement
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);
        return contentType;
    }

    private async Task<XElement> ExportBlueprintAndDeleteAsync(IContentType contentType)
    {
        var contentService = GetRequiredService<IContentService>();

        // saving through the content service isn't validated, so we can make a blueprint for any type.
        var blueprint = contentService.CreateBlueprintFromContent(contentService.Create("source", -1, contentType), "My blueprint");
        contentService.SaveBlueprint(blueprint, null);

        var node = (await Serializer.SerializeAsync(blueprint, new SyncSerializerOptions())).Item;
        contentService.DeleteBlueprint(blueprint);
        return node;
    }

    [Test]
    public async Task Import_BlueprintForDocumentType_Imports()
    {
        var node = await ExportBlueprintAndDeleteAsync(await CreateContentType("blueprintPage", isElement: false));

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(GetRequiredService<IContentService>().GetBlueprintById(attempt.Item.Key), Is.Not.Null);
        });
    }

    [Test]
    public async Task Import_BlueprintForElementType_FailsCleanly()
    {
        var node = await ExportBlueprintAndDeleteAsync(await CreateContentType("blueprintElement", isElement: true));

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.Change, Is.EqualTo(ChangeType.ImportFail));
            Assert.That(attempt.Message, Does.Contain("not allowed for blueprints"));
        });
    }
}
