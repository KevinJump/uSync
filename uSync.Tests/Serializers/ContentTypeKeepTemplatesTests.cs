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

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  with KeepTemplates on, a site-only allowed template is kept even when
///  its alias is a substring of a template in the file (#1249).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentTypeKeepTemplatesTests : UmbracoIntegrationTest
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

    /// <summary>
    ///  serializes a 'page' doctype that allows only homePage, then adds
    ///  the site-only template to the saved doctype (not the file).
    /// </summary>
    private async Task<(IContentType contentType, XElement node)> SetupAsync(string siteOnlyAlias)
    {
        var homePage = await CreateTemplate("homePage");
        var siteOnly = await CreateTemplate(siteOnlyAlias);

        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1)
        {
            Alias = "page",
            Name = "Page",
            AllowedTemplates = [homePage]
        };
        await ContentTypeService.CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var node = (await Serializer.SerializeAsync(ContentTypeService.Get(contentType.Key), new SyncSerializerOptions())).Item;
        Assert.That(node.Element("Info")!.Element("AllowedTemplates")!.Elements("Template").Select(x => x.Value), Is.EqualTo(new[] { "homePage" }));

        var saved = ContentTypeService.Get(contentType.Key)!;
        saved.AllowedTemplates = [homePage, siteOnly];
        await ContentTypeService.UpdateAsync(saved, UmbConstants.Security.SuperUserKey);

        return (saved, node);
    }

    private async Task<string[]> ImportAsync(IContentType contentType, XElement node, Dictionary<string, object> settings)
    {
        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass, settings));
        Assert.That(attempt.Success, Is.True, attempt.Message);

        return ContentTypeService.Get(contentType.Key)!.AllowedTemplates!.Select(x => x.Alias).Order().ToArray();
    }

    [Test]
    public async Task Import_KeepTemplates_KeepsSiteOnlyTemplateWhoseAliasIsSubstring()
    {
        var (contentType, node) = await SetupAsync("home");

        var aliases = await ImportAsync(contentType, node, new() { ["KeepTemplates"] = true });

        Assert.That(aliases, Is.EqualTo(new[] { "home", "homePage" }));
    }

    [Test]
    public async Task Import_KeepTemplates_KeepsSiteOnlyTemplate()
    {
        var (contentType, node) = await SetupAsync("other");

        var aliases = await ImportAsync(contentType, node, new() { ["KeepTemplates"] = true });

        Assert.That(aliases, Is.EqualTo(new[] { "homePage", "other" }));
    }

    [Test]
    public async Task Import_WithoutKeepTemplates_UsesFileTemplatesOnly()
    {
        var (contentType, node) = await SetupAsync("home");

        var aliases = await ImportAsync(contentType, node, []);

        Assert.That(aliases, Is.EqualTo(new[] { "homePage" }));
    }
}
