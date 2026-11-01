using System;
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
using Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_15_0_0.LocalLinks;
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
///  culture codes in content files, across Umbraco versions (17.7 normalises
///  culture casing when content is loaded, earlier versions do not).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentCultureTests : UmbracoIntegrationTest
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

    private Task<IContent> CreateVariantContent(bool withSharedProperty = false)
        => CreateContent(ContentVariation.Culture, withSharedProperty, content =>
        {
            content.SetValue("title", "title en", "en-US");
            content.SetValue("title", "title da", "da-DK");
        });

    private async Task<IContent> CreateContent(ContentVariation variation, bool withSharedProperty, Action<IContent> setValues)
    {
        await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), UmbConstants.Security.SuperUserKey);

        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = "variantPage",
            Name = "Variant Page",
            Variations = variation
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            Variations = variation,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        if (withSharedProperty)
        {
            contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "shared")
            {
                Name = "Shared",
                Variations = ContentVariation.Nothing,
                DataTypeId = UmbConstants.DataTypes.Textbox
            });
        }
        GetRequiredService<IContentTypeService>().Save(contentType);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("page", -1, contentType);
        if (variation != ContentVariation.Nothing)
        {
            content.SetCultureName("page en", "en-US");
            content.SetCultureName("page da", "da-DK");
        }
        setValues(content);
        if (withSharedProperty)
            content.SetValue("shared", "shared value");
        contentService.Save(content);
        return content;
    }

    private static string[] Cultures(XElement node)
        => [.. node.Descendants().Attributes("Culture").Select(x => x.Value).Distinct().Order()];

    private async Task<XElement> ExportEnglishOnly(Dictionary<string, object?> settings)
    {
        var content = await CreateVariantContent(withSharedProperty: true);
        settings[Core.uSyncConstants.CultureKey] = "en-US";
        return (await Serializer.SerializeAsync(content, new SyncSerializerOptions(settings))).Item;
    }

    private static string[] SharedValues(XElement node)
        => [.. node.Element("Properties")?.Element("shared")?.Elements("Value")
            .Where(x => x.Attribute("Culture") is null)
            .Select(x => x.Value) ?? []];

    [Test]
    public async Task Export_PartialCultureWithoutDefaultValuesSetting_OmitsDefaultValues()
    {
        var node = await ExportEnglishOnly([]);

        Assert.Multiple(() =>
        {
            Assert.That(node.Attribute(Core.uSyncConstants.DefaultsKey), Is.Null);
            Assert.That(SharedValues(node), Is.Empty);
        });
    }

    [Test]
    public async Task Export_PartialCultureWithDefaultValuesSetting_WritesDefaultValues()
    {
        var node = await ExportEnglishOnly(new() { [Core.uSyncConstants.DefaultsKey] = "true" });

        Assert.Multiple(() =>
        {
            Assert.That(node.Attribute(Core.uSyncConstants.DefaultsKey)?.Value, Is.EqualTo("true"));
            Assert.That(SharedValues(node), Is.EqualTo(new[] { "shared value" }));
        });
    }

    private async Task<XElement> ExportWith(IContent content, string key, string value)
    {
        var options = new SyncSerializerOptions(new Dictionary<string, object?> { [key] = value });
        return (await Serializer.SerializeAsync(content, options)).Item;
    }

    private Task Import(XElement node)
        => Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));

    private static IEnumerable<string> Values(XElement node)
        => node.Descendants("Value").Select(x => x.Value);

    [Test]
    public async Task Export_InvariantItemWithCulturesSetting_KeepsValues()
    {
        var content = await CreateContent(ContentVariation.Nothing, false, x => x.SetValue("title", "source"));
        var node = await ExportWith(content, Core.uSyncConstants.CultureKey, "en-US");

        content.SetValue("title", "target");
        GetRequiredService<IContentService>().Save(content);
        await Import(node);

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(Values(node), Is.EqualTo(new[] { "source" }));
            Assert.That(reloaded.GetValue<string>("title"), Is.EqualTo("source"));
        });
    }

    [Test]
    public async Task Export_PartialCultureWithMissingValue_BlanksValueOnImport()
    {
        var content = await CreateContent(ContentVariation.Culture, false, x => x.SetValue("title", "title da", "da-DK"));
        var node = await ExportWith(content, Core.uSyncConstants.CultureKey, "en-US");

        content.SetValue("title", "stale", "en-US");
        GetRequiredService<IContentService>().Save(content);
        await Import(node);

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(Cultures(node), Is.EqualTo(new[] { "en-US" }));
            Assert.That(reloaded.GetValue<string>("title", "en-US"), Is.Null.Or.Empty);
            Assert.That(reloaded.GetValue<string>("title", "da-DK"), Is.EqualTo("title da"));
        });
    }

    [Test]
    public async Task Export_PartialCulture_LeavesOutSegmentValuesOfOtherCultures()
    {
        var content = await CreateContent(ContentVariation.CultureAndSegment, false,
            x => x.SetValue("title", "title da", "da-DK", "seg1"));
        var node = await ExportWith(content, Core.uSyncConstants.CultureKey, "en-US");

        await Import(node);

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(Values(node), Has.None.EqualTo("title da"));
            Assert.That(reloaded.GetValue<string>("title", "en-US", "seg1"), Is.Null);
        });
    }

    [Test]
    public async Task Export_PartialSegment_LeavesOutOtherSegments()
    {
        var content = await CreateContent(ContentVariation.CultureAndSegment, false, x =>
        {
            x.SetValue("title", "default", "en-US");
            x.SetValue("title", "one", "en-US", "seg1");
            x.SetValue("title", "two", "en-US", "seg2");
        });
        var node = await ExportWith(content, Core.uSyncConstants.SegmentKey, "seg1");

        await Import(node);

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(Values(node), Has.None.EqualTo("two"));
            Assert.That(reloaded.GetValue<string>("title", "en-US"), Is.EqualTo("default"));
        });
    }

    [Test]
    public async Task Export_WildcardCultures_IsNotPartial()
    {
        var content = await CreateVariantContent(withSharedProperty: true);
        var node = await ExportWith(content, Core.uSyncConstants.CultureKey, "*");

        Assert.Multiple(() =>
        {
            Assert.That(SharedValues(node), Is.EqualTo(new[] { "shared value" }));
            Assert.That(Cultures(node), Is.EqualTo(new[] { "da-DK", "en-US" }));
            Assert.That(node.Attribute(Core.uSyncConstants.CultureKey), Is.Null);
        });
    }

    [Test]
    public async Task Export_LanguageStoredInLowerCase_WritesStandardCasing()
    {
        var content = await CreateVariantContent();

        // a language row can be stored with any casing (neither Umbraco nor uSync normalise it).
        using (var scope = GetRequiredService<IScopeProvider>().CreateScope())
        {
            scope.Database.Execute("UPDATE umbracoLanguage SET languageISOCode='da-dk' WHERE languageISOCode='da-DK'");
            scope.Complete();
        }
        GetRequiredService<AppCaches>().RuntimeCache.Clear();
        GetRequiredService<AppCaches>().IsolatedCaches.ClearAllCaches();

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        var attempt = await Serializer.SerializeAsync(reloaded, new SyncSerializerOptions());

        Assert.That(Cultures(attempt.Item), Is.EqualTo(new[] { "da-DK", "en-US" }));
    }

    [Test]
    public async Task Import_CultureInOtherCasing_Imports()
    {
        var content = await CreateVariantContent();
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        foreach (var attribute in node.Descendants().Attributes("Culture").Where(x => x.Value == "da-DK"))
            attribute.Value = "DA-dk";
        node.Descendants("Name").Single(x => x.Attribute("Culture")?.Value == "DA-dk").Value = "changed name";

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force));

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(reloaded.GetCultureName("da-DK"), Is.EqualTo("changed name"));
        });
    }

    [TestCase("fr-FR", Description = "valid culture, not installed on the site")]
    [TestCase("not_a/culture", Description = "not a culture code")]
    public async Task Import_UnknownCulture_FailsCleanly(string culture)
    {
        var content = await CreateVariantContent();
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        foreach (var attribute in node.Descendants().Attributes("Culture").Where(x => x.Value == "da-DK"))
            attribute.Value = culture;
        node.Descendants("Name").Single(x => x.Attribute("Culture")?.Value == culture).Value = "changed name";

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();
        var actions = await handler.ImportElementAsync(node, "page.config", handler.DefaultConfig,
            new uSyncImportOptions { Flags = SerializerFlags.Force });

        var reloaded = GetRequiredService<IContentService>().GetById(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(actions.Single().Success, Is.False);
            Assert.That(actions.Single().Change, Is.EqualTo(ChangeType.Fail));
            Assert.That(reloaded.GetCultureName("da-DK"), Is.EqualTo("page da"), "existing content untouched");
        });
    }
}
