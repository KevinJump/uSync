using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Cms.Infrastructure.Sync;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Extensions;

using uSync.BackOffice;
using uSync.Core.Serialization;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  a content type import has to leave the published cache up to date. before Umbraco 18.2
///  core missed two cases (#23433, #23445), which the serializer works around.
/// </summary>
/// <remarks>
///  run with and without a real (deep cloning) runtime cache, because the variance bug
///  depends on the content type coming back out of the cache.
/// </remarks>
[TestFixture(true)]
[TestFixture(false)]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypeCacheRefreshTests : UmbracoIntegrationTest
{
    private readonly bool _useRealCache;

    public ContentTypeCacheRefreshTests(bool useRealCache) => _useRealCache = useRealCache;

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
        if (_useRealCache)
        {
            builder.Services.AddUnique(_ => new AppCaches(
                new DeepCloneAppCache(new ObjectCacheAppCache()),
                NoAppCache.Instance,
                new IsolatedCaches(_ => new DeepCloneAppCache(new ObjectCacheAppCache()))));
        }

        // the test host doesn't wire changes through to the published cache, so do what a site does.
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<ContentTypeChangedNotification, ContentTypeChangedDistributedCacheNotificationHandler>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();

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

    private ContentType BuildContentType(string alias, string propertyAlias, ContentVariation variation = ContentVariation.Nothing)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = alias, Name = alias, Variations = variation };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, propertyAlias)
        {
            Name = propertyAlias,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        return contentType;
    }

    private async Task ImportAsync(XElement node)
    {
        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));
        Assert.That(attempt.Success, Is.True, attempt.Message);
    }

    [Test]
    public async Task AddComposition_ToParent_RefreshesChildPublishedType()
    {
        var composition = BuildContentType("composition", "compositionProp");
        await ContentTypeService.CreateAsync(composition, UmbConstants.Security.SuperUserKey);

        var parent = BuildContentType("parent", "parentProp");
        await ContentTypeService.CreateAsync(parent, UmbConstants.Security.SuperUserKey);

        var child = BuildContentType("child", "childProp");
        child.ParentId = parent.Id;
        child.AddContentType(parent);
        await ContentTypeService.CreateAsync(child, UmbConstants.Security.SuperUserKey);

        // load the child's published type, so there is something to go stale.
        var publishedTypes = GetRequiredService<IPublishedContentTypeCache>();
        Assert.That(publishedTypes.Get(PublishedItemType.Content, "child").GetPropertyType("compositionProp"), Is.Null);

        var node = (await Serializer.SerializeAsync(ContentTypeService.Get(parent.Key), new SyncSerializerOptions())).Item;
        node.Element("Info").Element("Compositions").Add(new XElement("Composition", "composition", new XAttribute("Key", composition.Key)));
        await ImportAsync(node);

        Assert.That(publishedTypes.Get(PublishedItemType.Content, "child").GetPropertyType("compositionProp"), Is.Not.Null);
    }

    [Test]
    public async Task PropertyVarianceChange_RefreshesPublishedContent()
    {
        await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), UmbConstants.Security.SuperUserKey);

        // the content type varies by culture, but the property doesn't (yet).
        var contentType = BuildContentType("variantPage", "title", ContentVariation.Culture);
        await ContentTypeService.CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("page", -1, contentType);
        content.SetCultureName("page en", "en-US");
        content.SetCultureName("page da", "da-DK");
        content.SetValue("title", "invariant title");
        contentService.Save(content);
        contentService.Publish(content, ["*"]);

        GetRequiredService<IVariationContextAccessor>().VariationContext = new VariationContext("en-US");
        var publishedContent = GetRequiredService<IPublishedContentCache>();
        Assert.That((await publishedContent.GetByIdAsync(content.Key))!.Value<string>("title", "da-DK"), Is.EqualTo("invariant title"));

        var node = (await Serializer.SerializeAsync(ContentTypeService.Get(contentType.Key), new SyncSerializerOptions())).Item;
        node.Descendants("GenericProperty").Single(x => x.Element("Alias")?.Value == "title").Element("Variations").Value = "Culture";
        await ImportAsync(node);

        // Umbraco moves the invariant value to the default culture, which a stale cache doesn't have.
        var updated = await publishedContent.GetByIdAsync(content.Key);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Value<string>("title", "en-US"), Is.EqualTo("invariant title"));
            Assert.That(updated!.Value<string>("title", "da-DK"), Is.Empty);
        });
    }

    private sealed class LocalServerMessenger : ServerMessengerBase
    {
        public LocalServerMessenger()
            : base(false, new SystemTextJsonSerializer(new DefaultJsonSerializerEncoderFactory()))
        { }

        public override void SendMessages() { }

        public override void Sync() { }

        protected override void DeliverRemote(ICacheRefresher refresher, MessageType messageType, IEnumerable<object> ids = null, string json = null) { }
    }
}
