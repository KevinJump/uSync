using System;
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
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.Tests.Handlers;

/// <summary>
///  importing content that isn't in Umbraco yet - items under a parent the import created
///  are created without looking for siblings by name, items under an existing parent still
///  match an existing item by name.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class FirstImportTests : UmbracoIntegrationTest
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
        new ConvertLocalLinkComposer().Compose(builder);

        builder.AdduSync(settings =>
        {
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "None";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    private const string _root = "uSync/tests/firstimport";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private ContentHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentType CreateContentType()
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1)
        {
            Alias = "page",
            Name = "Page",
            AllowedAsRoot = true
        };
        GetRequiredService<IContentTypeService>().Save(contentType);
        return contentType;
    }

    private IContent Add(IContentType contentType, string name, IContent? parent)
    {
        var content = ContentService.Create(name, parent?.Id ?? -1, contentType);
        ContentService.Save(content);
        return content;
    }

    private async Task<string> ExportAll()
    {
        var folder = $"{_root}/{Guid.NewGuid():N}";
        await Handler.ExportAllAsync([folder], Handler.DefaultConfig, null);
        return folder;
    }

    [Test]
    public async Task Import_IntoAnEmptySite_CreatesTheTree()
    {
        var contentType = CreateContentType();
        var home = Add(contentType, "home", null);
        var news = Add(contentType, "news", home);
        var children = Enumerable.Range(1, 5).Select(x => Add(contentType, $"story {x}", news)).ToList();
        var grandchild = Add(contentType, "comments", children[0]);

        var folder = await ExportAll();
        ContentService.Delete(home);

        var actions = await Handler.ImportAllAsync([folder], Handler.DefaultConfig, new uSyncImportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(actions.Where(x => !x.Success), Is.Empty);

            var importedNews = ContentService.GetById(news.Key);
            Assert.That(importedNews, Is.Not.Null);
            Assert.That(ContentService.GetById(importedNews!.ParentId)?.Key, Is.EqualTo(home.Key));

            foreach (var child in children)
                Assert.That(ContentService.GetById(child.Key)?.ParentId, Is.EqualTo(importedNews.Id), child.Name);

            Assert.That(ContentService.GetById(grandchild.Key)?.ParentId,
                Is.EqualTo(ContentService.GetById(children[0].Key)?.Id));

            Assert.That(ContentService.CountChildren(importedNews.Id), Is.EqualTo(5));
        });
    }

    [Test]
    public async Task Import_UnderAnExistingParent_StillMatchesAnExistingItemByName()
    {
        var contentType = CreateContentType();
        var home = Add(contentType, "home", null);
        var original = Add(contentType, "about", home);

        var folder = await ExportAll();

        // replace the child with one that has the same name, but a different key.
        ContentService.Delete(original);
        var replacement = Add(contentType, "about", home);

        var actions = await Handler.ImportAllAsync([folder], Handler.DefaultConfig, new uSyncImportOptions());

        Assert.Multiple(() =>
        {
            Assert.That(actions.Where(x => !x.Success), Is.Empty);
            Assert.That(ContentService.CountChildren(home.Id), Is.EqualTo(1), "should match the existing 'about', not add another");
            Assert.That(ContentService.GetById(original.Key), Is.Not.Null, "the matched item takes the key from the file");
        });
    }
}
