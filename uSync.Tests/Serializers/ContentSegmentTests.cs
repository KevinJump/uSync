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
using uSync.Core.Serialization;
using uSyncConstants = uSync.Core.uSyncConstants;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  segment values in content files. v19 treats segments as overrides of the
///  default (null) segment, for each culture (Umbraco #23060).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentSegmentTests : UmbracoIntegrationTest
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
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("ContentSerializer");

    private IContentService ContentService => GetRequiredService<IContentService>();

    private async Task<IContent> CreateContent(bool variesByCulture)
    {
        if (variesByCulture)
            await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), UmbConstants.Security.SuperUserKey);

        var variation = variesByCulture
            ? ContentVariation.CultureAndSegment
            : ContentVariation.Segment;

        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = "segmentPage",
            Name = "Segment Page",
            Variations = variation
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            Variations = variation,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "plain")
        {
            Name = "Plain",
            Variations = variesByCulture ? ContentVariation.Culture : ContentVariation.Nothing,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var content = ContentService.Create("page", -1, contentType);
        if (variesByCulture)
        {
            foreach (var culture in new[] { "en-US", "da-DK" })
            {
                content.SetCultureName($"page {culture}", culture);
                content.SetValue("title", $"title {culture}", culture);
                content.SetValue("title", $"vip {culture}", culture, "vip");
            }
        }
        else
        {
            content.SetValue("title", "title");
            content.SetValue("title", "vip", segment: "vip");
        }

        ContentService.Save(content);
        return content;
    }

    private static IEnumerable<string> TitleValues(XElement node)
        => node.Element("Properties")!.Element("title")!.Elements("Value")
            .Select(x => $"{x.Attribute("Culture")?.Value}|{x.Attribute("Segment")?.Value}|{x.Value}");

    [Test]
    public async Task CultureAndSegment_Exports_EachCultureWithItsSegments()
    {
        var content = await CreateContent(variesByCulture: true);

        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        Assert.That(TitleValues(node), Is.EqualTo(new[] {
            "da-DK||title da-DK", "da-DK|vip|vip da-DK",
            "en-US||title en-US", "en-US|vip|vip en-US" }));
    }

    [Test]
    public async Task CultureAndSegment_PartialCultureExport_OnlyIncludesThatCulturesSegments()
    {
        var content = await CreateContent(variesByCulture: true);

        var options = new SyncSerializerOptions(new Dictionary<string, object> { [uSyncConstants.CultureKey] = "en-US" });
        var node = (await Serializer.SerializeAsync(content, options)).Item;

        Assert.That(TitleValues(node), Is.EqualTo(new[] { "en-US||title en-US", "en-US|vip|vip en-US" }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Segments_RoundTrip(bool variesByCulture)
    {
        var content = await CreateContent(variesByCulture);
        var culture = variesByCulture ? "en-US" : null;
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        // change the site after the export: edit an override and add a new one.
        content.SetValue("title", "changed", culture, "vip");
        content.SetValue("title", "new override", culture, "other");
        ContentService.Save(content);

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));
        var reloaded = ContentService.GetById(content.Key)!;

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(reloaded.GetValue("title", culture, "vip"), Is.EqualTo(variesByCulture ? "vip en-US" : "vip"));
            Assert.That(reloaded.GetValue("title", culture, "other"), Is.Null, "override not in the file is cleared");
        });

        // and the import gives the same file back.
        var reExport = (await Serializer.SerializeAsync(reloaded, new SyncSerializerOptions())).Item;
        Assert.That(TitleValues(reExport).Where(x => !x.Contains("|other|")), Is.EqualTo(TitleValues(node)));
    }

    [Test]
    public async Task KeepMissingSegments_LeavesOverridesNotInTheFile()
    {
        var content = await CreateContent(variesByCulture: false);
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        content.SetValue("title", "new override", segment: "other");
        ContentService.Save(content);

        var options = new SyncSerializerOptions(SerializerFlags.Force,
            new Dictionary<string, object> { [uSyncConstants.DefaultSettings.KeepMissingSegments] = true });
        var attempt = await Serializer.DeserializeAsync(node, options);

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(ContentService.GetById(content.Key)!.GetValue("title", segment: "other"), Is.EqualTo("new override"));
        });
    }

    [Test]
    public async Task PartialCultureImport_LeavesOtherCulturesSegments()
    {
        var content = await CreateContent(variesByCulture: true);

        var options = new SyncSerializerOptions(new Dictionary<string, object> { [uSyncConstants.CultureKey] = "en-US" });
        var node = (await Serializer.SerializeAsync(content, options)).Item;

        content.SetValue("title", "da other", "da-DK", "other");
        ContentService.Save(content);

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(ContentService.GetById(content.Key)!.GetValue("title", "da-DK", "other"), Is.EqualTo("da other"));
        });
    }

    [Test]
    public async Task SegmentValue_ForPropertyWithoutSegmentVariance_IsSkipped()
    {
        var content = await CreateContent(variesByCulture: false);
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        var plain = node.Element("Properties")!.Element("plain")!;
        plain.Add(new XElement("Value", new XAttribute("Segment", "vip"), new XCData("segment value")));
        plain.Elements("Value").First().ReplaceNodes(new XCData("default value"));

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));
        var reloaded = ContentService.GetById(content.Key)!;

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(attempt.Message ?? string.Empty, Does.Not.Contain("Failed to set"));
            Assert.That(reloaded.GetValue("plain"), Is.EqualTo("default value"));
        });
    }
}
