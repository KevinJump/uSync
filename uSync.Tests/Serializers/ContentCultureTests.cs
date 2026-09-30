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
///  culture codes in content files, across Umbraco versions (18.2 normalises
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

    private async Task<IContent> CreateVariantContent()
    {
        await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), UmbConstants.Security.SuperUserKey);

        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = "variantPage",
            Name = "Variant Page",
            Variations = ContentVariation.Culture
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            Variations = ContentVariation.Culture,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("page", -1, contentType);
        content.SetCultureName("page en", "en-US");
        content.SetCultureName("page da", "da-DK");
        content.SetValue("title", "title en", "en-US");
        content.SetValue("title", "title da", "da-DK");
        contentService.Save(content);
        return content;
    }

    private static string[] Cultures(XElement node)
        => [.. node.Descendants().Attributes("Culture").Select(x => x.Value).Distinct().Order()];

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
