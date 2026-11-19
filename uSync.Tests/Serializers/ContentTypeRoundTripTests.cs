using System;
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
///  document type schema edge cases survive a round trip through the
///  serializer, and the site reports no changes afterwards (#1242).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypeRoundTripTests : UmbracoIntegrationTest
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

    private ITemplateService TemplateService => GetRequiredService<ITemplateService>();

    private ISyncSerializer<IContentType> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContentType>("ContentTypeSerializer");

    private readonly List<string> _templates = [];

    // creating a template writes a view into the test project, so remove them again.
    [TearDown]
    public async Task RemoveTemplates()
    {
        foreach (var contentType in ContentTypeService.GetAll().ToList())
            ContentTypeService.Delete(contentType);

        foreach (var alias in _templates)
            await TemplateService.DeleteAsync(alias, UmbConstants.Security.SuperUserKey);

        _templates.Clear();
    }

    private async Task<ITemplate> CreateTemplate(string alias)
    {
        var attempt = await TemplateService.CreateAsync(alias, alias, null, UmbConstants.Security.SuperUserKey);
        Assert.That(attempt.Success, Is.True, attempt.Status.ToString());
        _templates.Add(alias);
        return attempt.Result;
    }

    private PropertyType TextBox(string alias)
        => new(GetRequiredService<IShortStringHelper>(), "Umbraco.TextBox", ValueStorageType.Nvarchar, alias)
        {
            Name = alias,
            DataTypeId = UmbConstants.DataTypes.Textbox
        };

    private ContentType CreateContentType(string alias)
        => new(GetRequiredService<IShortStringHelper>(), -1) { Alias = alias, Name = alias };

    private async Task<XElement> SerializeAsync(IContentType contentType)
        => (await Serializer.SerializeAsync(ContentTypeService.Get(contentType.Key), new SyncSerializerOptions())).Item;

    private void UpdateSite(IContentType contentType, Action<IContentType> update)
    {
        var saved = ContentTypeService.Get(contentType.Key)!;
        update(saved);
        ContentTypeService.Save(saved);
    }

    /// <summary>
    ///  imports the node, checks it worked and that the site now matches the file.
    /// </summary>
    private async Task<IContentType> ImportAsync(Guid key, XElement node)
    {
        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));
        Assert.That(attempt.Success, Is.True, attempt.Message);

        Assert.That(await Serializer.IsCurrentAsync(new XElement(node), new SyncSerializerOptions()), Is.EqualTo(ChangeType.NoChange));

        return ContentTypeService.Get(key)!;
    }

    private static string[] GroupOrder(IContentType contentType, string group)
        => contentType.PropertyGroups[group]!.PropertyTypes!.OrderBy(x => x.SortOrder).Select(x => x.Alias).ToArray();

    private static string[] AllowedOrder(IContentType contentType)
        => contentType.AllowedContentTypes!.OrderBy(x => x.SortOrder).Select(x => x.Alias).ToArray();

    private static string[] AllowedTemplates(IContentType contentType)
        => contentType.AllowedTemplates!.Select(x => x.Alias).Order().ToArray();

    [Test]
    public async Task Import_PropertyOrderInGroup_MatchesFile()
    {
        var contentType = CreateContentType("rtPropertyOrder");
        contentType.AddPropertyType(TextBox("first"), "content", "Content");
        contentType.AddPropertyType(TextBox("second"), "content", "Content");
        contentType.AddPropertyType(TextBox("third"), "content", "Content");
        var order = 0;
        foreach (var property in contentType.PropertyGroups["content"]!.PropertyTypes!)
            property.SortOrder = order++;
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);

        UpdateSite(contentType, site =>
        {
            site.PropertyTypes.First(x => x.Alias == "first").SortOrder = 2;
            site.PropertyTypes.First(x => x.Alias == "third").SortOrder = 0;
        });
        Assert.That(GroupOrder(ContentTypeService.Get(contentType.Key)!, "content"), Is.EqualTo(new[] { "third", "second", "first" }));

        var imported = await ImportAsync(contentType.Key, node);

        Assert.That(GroupOrder(imported, "content"), Is.EqualTo(new[] { "first", "second", "third" }));
    }

    private IContentType[] CreateChildren(string prefix)
    {
        var children = new[] { $"{prefix}One", $"{prefix}Two", $"{prefix}Three" }
            .Select(CreateContentType)
            .ToArray();

        foreach (var child in children)
            ContentTypeService.Save(child);

        return children;
    }

    private static ContentTypeSort[] Sorted(params IContentType[] items)
        => items.Select((x, i) => new ContentTypeSort(x.Key, i, x.Alias)).ToArray();

    [Test]
    public async Task Import_AllowedChildren_DifferentSetAndOrder_MatchesFile()
    {
        var children = CreateChildren("rtChildA");
        var parent = CreateContentType("rtParentA");
        parent.AllowedContentTypes = Sorted(children);
        ContentTypeService.Save(parent);

        var node = await SerializeAsync(parent);

        UpdateSite(parent, site => site.AllowedContentTypes = Sorted(children[2], children[0]));
        Assert.That(AllowedOrder(ContentTypeService.Get(parent.Key)!), Is.EqualTo(new[] { "rtChildAThree", "rtChildAOne" }));

        var imported = await ImportAsync(parent.Key, node);

        Assert.That(AllowedOrder(imported), Is.EqualTo(new[] { "rtChildAOne", "rtChildATwo", "rtChildAThree" }));
    }

    // covers the re-order dirty fix in ContentTypeBaseSerializer.DeserializeStructureAsync.
    [Test]
    public async Task Import_AllowedChildren_SameSetDifferentOrder_MatchesFile()
    {
        var children = CreateChildren("rtChildB");
        var parent = CreateContentType("rtParentB");
        parent.AllowedContentTypes = Sorted(children);
        ContentTypeService.Save(parent);

        var node = await SerializeAsync(parent);

        UpdateSite(parent, site => site.AllowedContentTypes = Sorted(children[2], children[1], children[0]));
        Assert.That(AllowedOrder(ContentTypeService.Get(parent.Key)!), Is.EqualTo(new[] { "rtChildBThree", "rtChildBTwo", "rtChildBOne" }));

        var imported = await ImportAsync(parent.Key, node);

        Assert.That(AllowedOrder(imported), Is.EqualTo(new[] { "rtChildBOne", "rtChildBTwo", "rtChildBThree" }));
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task Import_EmptyDescriptions_StayEmpty(string description)
    {
        var contentType = CreateContentType("rtDescription");
        contentType.Description = description;
        var property = TextBox("title");
        property.Description = description;
        contentType.AddPropertyType(property, "content", "Content");
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);

        ContentTypeService.Delete(ContentTypeService.Get(contentType.Key)!);

        var imported = await ImportAsync(contentType.Key, node);

        var importedProperty = imported.PropertyTypes.First(x => x.Alias == "title");
        Assert.Multiple(() =>
        {
            Assert.That(imported.Description, Is.Null.Or.Empty);
            Assert.That(imported.Description, Is.Not.EqualTo("null"));
            Assert.That(importedProperty.Description, Is.Null.Or.Empty);
            Assert.That(importedProperty.Description, Is.Not.EqualTo("null"));
        });
    }

    private async Task<(IContentType contentType, XElement node)> SetupValidationAsync(string mandatoryMessage, string regExMessage)
    {
        var contentType = CreateContentType("rtValidation");
        var property = TextBox("code");
        property.Mandatory = true;
        property.MandatoryMessage = mandatoryMessage;
        property.ValidationRegExp = "^[a-z]+$";
        property.ValidationRegExpMessage = regExMessage;
        contentType.AddPropertyType(property, "content", "Content");
        ContentTypeService.Save(contentType);

        return (contentType, await SerializeAsync(contentType));
    }

    [Test]
    public async Task Import_ChangedValidationMessages_MatchFile()
    {
        var (contentType, node) = await SetupValidationAsync("file mandatory", "file regex");

        UpdateSite(contentType, site =>
        {
            var property = site.PropertyTypes.First(x => x.Alias == "code");
            property.MandatoryMessage = "site mandatory";
            property.ValidationRegExpMessage = "site regex";
        });

        var imported = await ImportAsync(contentType.Key, node);

        var importedProperty = imported.PropertyTypes.First(x => x.Alias == "code");
        Assert.Multiple(() =>
        {
            Assert.That(importedProperty.Mandatory, Is.True);
            Assert.That(importedProperty.ValidationRegExp, Is.EqualTo("^[a-z]+$"));
            Assert.That(importedProperty.MandatoryMessage, Is.EqualTo("file mandatory"));
            Assert.That(importedProperty.ValidationRegExpMessage, Is.EqualTo("file regex"));
        });
    }

    [Test]
    public async Task Import_ClearedValidationMessages_MatchFile()
    {
        var (contentType, node) = await SetupValidationAsync(null, null);

        UpdateSite(contentType, site =>
        {
            var property = site.PropertyTypes.First(x => x.Alias == "code");
            property.MandatoryMessage = "site mandatory";
            property.ValidationRegExpMessage = "site regex";
        });

        var imported = await ImportAsync(contentType.Key, node);

        var importedProperty = imported.PropertyTypes.First(x => x.Alias == "code");
        Assert.Multiple(() =>
        {
            Assert.That(importedProperty.MandatoryMessage, Is.Null.Or.Empty);
            Assert.That(importedProperty.ValidationRegExpMessage, Is.Null.Or.Empty);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Import_LabelOnTop_MatchesFile(bool fileValue)
    {
        var contentType = CreateContentType("rtLabelOnTop");
        var property = TextBox("title");
        property.LabelOnTop = fileValue;
        contentType.AddPropertyType(property, "content", "Content");
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);

        UpdateSite(contentType, site => site.PropertyTypes.First(x => x.Alias == "title").LabelOnTop = !fileValue);

        var imported = await ImportAsync(contentType.Key, node);

        Assert.That(imported.PropertyTypes.First(x => x.Alias == "title").LabelOnTop, Is.EqualTo(fileValue));
    }

    [Test]
    public async Task Import_FileAllowsExtraTemplate_AddsIt()
    {
        var main = await CreateTemplate("rtAddMain");
        var extra = await CreateTemplate("rtAddExtra");

        var contentType = CreateContentType("rtTemplatesAdd");
        contentType.AllowedTemplates = [main, extra];
        contentType.SetDefaultTemplate(main);
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);

        UpdateSite(contentType, site => site.AllowedTemplates = [main]);
        Assert.That(AllowedTemplates(ContentTypeService.Get(contentType.Key)!), Is.EqualTo(new[] { "rtAddMain" }));

        var imported = await ImportAsync(contentType.Key, node);

        Assert.That(AllowedTemplates(imported), Is.EqualTo(new[] { "rtAddExtra", "rtAddMain" }));
    }

    [Test]
    public async Task Import_SiteAllowsExtraTemplate_RemovesIt()
    {
        var main = await CreateTemplate("rtRemoveMain");
        var extra = await CreateTemplate("rtRemoveExtra");

        var contentType = CreateContentType("rtTemplatesRemove");
        contentType.AllowedTemplates = [main];
        contentType.SetDefaultTemplate(main);
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);

        UpdateSite(contentType, site => site.AllowedTemplates = [main, extra]);
        Assert.That(AllowedTemplates(ContentTypeService.Get(contentType.Key)!), Is.EqualTo(new[] { "rtRemoveExtra", "rtRemoveMain" }));

        var imported = await ImportAsync(contentType.Key, node);

        Assert.That(AllowedTemplates(imported), Is.EqualTo(new[] { "rtRemoveMain" }));
    }

    [Test]
    public async Task Import_DifferentDefaultTemplate_SwitchesToFileDefault()
    {
        var first = await CreateTemplate("rtDefaultFirst");
        var second = await CreateTemplate("rtDefaultSecond");

        var contentType = CreateContentType("rtTemplatesDefault");
        contentType.AllowedTemplates = [first, second];
        contentType.SetDefaultTemplate(second);
        ContentTypeService.Save(contentType);

        var node = await SerializeAsync(contentType);
        Assert.That(node.Element("Info")!.Element("DefaultTemplate")!.Value, Is.EqualTo("rtDefaultSecond"));

        UpdateSite(contentType, site => site.SetDefaultTemplate(first));
        Assert.That(ContentTypeService.Get(contentType.Key)!.DefaultTemplate!.Alias, Is.EqualTo("rtDefaultFirst"));

        var imported = await ImportAsync(contentType.Key, node);

        Assert.Multiple(() =>
        {
            Assert.That(AllowedTemplates(imported), Is.EqualTo(new[] { "rtDefaultFirst", "rtDefaultSecond" }));
            Assert.That(imported.DefaultTemplate!.Alias, Is.EqualTo("rtDefaultSecond"));
        });
    }
}
