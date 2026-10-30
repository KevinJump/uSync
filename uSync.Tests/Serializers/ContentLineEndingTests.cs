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
using uSync.Core;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a value that only differs by its line endings (a file made on windows,
///  reported on linux, or the other way round) is not a change (#495).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentLineEndingTests : UmbracoIntegrationTest
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

    private ISyncSerializer<IContent> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("ContentSerializer");

    private IContent CreateContent(string notes)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page" };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextArea", ValueStorageType.Ntext, "notes")
        {
            Name = "Notes",
            DataTypeId = UmbConstants.DataTypes.Textarea
        });
        GetRequiredService<IContentTypeService>().Save(contentType);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("page", -1, contentType);
        content.SetValue("notes", notes);
        contentService.Save(content);
        return content;
    }

    private static XElement NotesValue(XElement node)
        => node.Element("Properties")!.Element("notes")!.Element("Value")!;

    [TestCase("line one\nline two", "\n", "\r\n", Description = "linux site, windows file")]
    [TestCase("line one\r\nline two", "\r\n", "\n", Description = "windows site, linux file")]
    public async Task IsCurrent_OnlyLineEndingsDiffer_IsNoChange(string notes, string from, string to)
    {
        var content = CreateContent(notes);
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        var value = NotesValue(node);
        Assert.That(value.Value, Does.Contain(from), "the export kept the site's line endings");
        value.ReplaceNodes(new XCData(value.Value.Replace(from, to)));

        Assert.That(await Serializer.IsCurrentAsync(node, new SyncSerializerOptions()), Is.EqualTo(ChangeType.NoChange));
    }

    [Test]
    public async Task IsCurrent_TextChanged_IsAChange()
    {
        var content = CreateContent("line one\nline two");
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        NotesValue(node).ReplaceNodes(new XCData("line one\nline three"));

        Assert.That(await Serializer.IsCurrentAsync(node, new SyncSerializerOptions()), Is.EqualTo(ChangeType.Update));
    }
}
