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
///  v19 schedules record whether a release includes the invariant values of
///  variant content (Umbraco #24022).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentScheduleTests : UmbracoIntegrationTest
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

    private async Task<IContent> CreatePublishedContent()
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
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "plain")
        {
            Name = "Plain",
            Variations = ContentVariation.Nothing,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var content = ContentService.Create("page", -1, contentType);
        foreach (var culture in new[] { "en-US", "da-DK" })
        {
            content.SetCultureName($"page {culture}", culture);
            content.SetValue("title", $"title {culture}", culture);
        }
        content.SetValue("plain", "original");
        ContentService.Save(content);
        ContentService.Publish(content, ["en-US", "da-DK"]);

        return ContentService.GetById(content.Key)!;
    }

    private static ContentScheduleCollection Schedule(ContentSchedule schedule)
    {
        var collection = new ContentScheduleCollection();
        collection.Add(schedule);
        return collection;
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Schedule_RoundTripsIncludeInvariantForVariant(bool includeInvariantForVariant)
    {
        var content = await CreatePublishedContent();
        var release = System.DateTime.Now.AddDays(1);
        ContentService.PersistContentSchedule(content, Schedule(new ContentSchedule("en-US", release, ContentScheduleAction.Release, includeInvariantForVariant)));

        var node = (await Serializer.SerializeAsync(ContentService.GetById(content.Key)!, new SyncSerializerOptions())).Item;
        var exported = node.Element(uSyncConstants.Xml.Info)!.Element("Schedule")!.Element("ContentSchedule")!;
        Assert.That(exported.Element("IncludeInvariantForVariant")?.Value, Is.EqualTo(includeInvariantForVariant.ToString().ToLowerInvariant()));

        // clear it on the site, and import it back.
        ContentService.PersistContentSchedule(content, new ContentScheduleCollection());

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));
        var schedule = ContentService.GetContentScheduleByContentId(content.Id).FullSchedule.Single();

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(schedule.IncludeInvariantForVariant, Is.EqualTo(includeInvariantForVariant));
        });
    }

    [Test]
    public async Task Schedule_FromBeforeV19_IncludesInvariantForVariant()
    {
        var content = await CreatePublishedContent();
        ContentService.PersistContentSchedule(content, Schedule(new ContentSchedule("en-US", System.DateTime.Now.AddDays(1), ContentScheduleAction.Release, false)));

        var node = (await Serializer.SerializeAsync(ContentService.GetById(content.Key)!, new SyncSerializerOptions())).Item;
        node.Element(uSyncConstants.Xml.Info)!.Element("Schedule")!.Element("ContentSchedule")!.Element("IncludeInvariantForVariant")!.Remove();
        ContentService.PersistContentSchedule(content, new ContentScheduleCollection());

        await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));

        Assert.That(ContentService.GetContentScheduleByContentId(content.Id).FullSchedule.Single().IncludeInvariantForVariant, Is.True);
    }
}
