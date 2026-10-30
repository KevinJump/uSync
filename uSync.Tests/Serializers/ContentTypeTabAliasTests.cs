using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a tab whose alias contains another tab's alias (previewContent / content)
///  survives an import onto a blank site (#882, fixed by #876).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypeTabAliasTests : UmbracoIntegrationTest
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
        // the RTE mapper needs the local link processor, which the test host doesn't register.
        new ConvertLocalLinkComposer().Compose(builder);

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
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContentType>("ContentTypeSerializer");

    private PropertyType TextBox(string alias)
        => new(GetRequiredService<IShortStringHelper>(), "Umbraco.TextBox", ValueStorageType.Nvarchar, alias)
        {
            Name = alias,
            DataTypeId = UmbConstants.DataTypes.Textbox
        };

    [Test]
    public async Task Import_OnBlankSite_KeepsTabWhoseAliasContainsAnother()
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1) { Alias = "blogPostBase", Name = "Blog Post Base" };
        contentType.AddPropertyType(TextBox("previewText"), "previewContent", "Preview Content");
        contentType.AddPropertyType(TextBox("bodyText"), "content", "Content");
        foreach (var group in contentType.PropertyGroups)
        {
            group.Type = PropertyGroupType.Tab;
            group.SortOrder = group.Alias == "previewContent" ? 0 : 1;
        }
        ContentTypeService.Save(contentType);

        var node = (await Serializer.SerializeAsync(ContentTypeService.Get(contentType.Key), new SyncSerializerOptions())).Item;

        // the clash only happens when previewContent is created before content is looked up.
        var tabs = node.Element("Tabs")!;
        var ordered = tabs.Elements("Tab").OrderByDescending(x => x.Element("Alias")!.Value).ToList();
        tabs.ReplaceNodes(ordered);
        Assert.That(ordered.Select(x => x.Element("Alias")!.Value), Is.EqualTo(new[] { "previewContent", "content" }));

        // first boot: the content type isn't there yet.
        ContentTypeService.Delete(ContentTypeService.Get(contentType.Key));

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));
        Assert.That(attempt.Success, Is.True, attempt.Message);

        var imported = ContentTypeService.Get(contentType.Key);
        Assert.Multiple(() =>
        {
            Assert.That(imported.PropertyGroups.Select(x => x.Alias).Order(), Is.EqualTo(new[] { "content", "previewContent" }));
            Assert.That(imported.PropertyGroups["previewContent"].PropertyTypes.Select(x => x.Alias), Is.EqualTo(new[] { "previewText" }));
            Assert.That(imported.PropertyGroups["content"].PropertyTypes.Select(x => x.Alias), Is.EqualTo(new[] { "bodyText" }));
        });
    }
}
