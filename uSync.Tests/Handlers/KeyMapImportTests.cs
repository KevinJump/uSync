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
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.Tests.Handlers;

/// <summary>
///  KeyConflict: Map - items that match by name but have a different key
///  keep their key, and references to them in other files are swapped.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class KeyMapImportTests : UmbracoIntegrationTest
{
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
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
            settings.KeyConflict = SyncKeyConflictMode.Map;
        });
    }

    private const string _root = "uSync/tests/keymap";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    [Test]
    public async Task Import_ParentWithDifferentKey_IsMapped_AndChildStaysUnderIt()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        await contentTypeService.CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
        await contentTypeService.UpdateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        // content is only matched by name under a parent, so home sits under a site root.
        var contentService = GetRequiredService<IContentService>();
        var site = contentService.Create("site", -1, contentType);
        contentService.Save(site);
        var home = contentService.Create("home", site.Id, contentType);
        contentService.Save(home);
        var about = contentService.Create("about", home.Id, contentType);
        contentService.Save(about);

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();
        var config = handler.DefaultConfig.Clone();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };
        var fileService = GetRequiredService<ISyncFileService>();

        var homeFile = (await handler.ExportAsync(home, folders, config)).Single(x => x.Success).FileName!;
        var aboutFile = (await handler.ExportAsync(about, folders, config)).Single(x => x.Success).FileName!;

        // the files come from a site where home has a different key.
        var fileKey = Guid.NewGuid();
        var homeNode = SwapKey(await fileService.LoadXElementAsync(homeFile), home.Key, fileKey);
        var aboutNode = SwapKey(await fileService.LoadXElementAsync(aboutFile), home.Key, fileKey);

        var keyMap = GetRequiredService<ISyncKeyMap>();
        Assert.That(keyMap.Enabled, Is.True);

        // scan first (as a full import does), then import the child before the parent
        // to prove the reference is swapped from the scan alone.
        await handler.ScanKeyConflictsAsync([homeNode, aboutNode]);

        Assert.That(keyMap.TryGetLocal(fileKey, out var mapped), Is.True);
        Assert.That(mapped, Is.EqualTo(home.Key));

        var aboutActions = (await handler.ImportElementAsync(aboutNode, aboutFile, config, new uSyncImportOptions { Flags = SerializerFlags.Force })).ToList();
        var homeActions = (await handler.ImportElementAsync(homeNode, homeFile, config, new uSyncImportOptions { Flags = SerializerFlags.Force })).ToList();

        var reloadedHome = contentService.GetById(home.Id)!;
        var reloadedAbout = contentService.GetById(about.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(homeActions.All(x => x.Success), Is.True, string.Join(", ", homeActions.Select(x => x.Message)));
            Assert.That(aboutActions.All(x => x.Success), Is.True, string.Join(", ", aboutActions.Select(x => x.Message)));
            Assert.That(reloadedHome.Key, Is.EqualTo(home.Key), "home keeps its key on this site");
            Assert.That(reloadedAbout.ParentId, Is.EqualTo(home.Id), "about is still under home");
            Assert.That(contentService.CountChildren(site.Id), Is.EqualTo(1), "no second home");
        });

        // with the keys mapped the report sees no changes.
        var report = (await handler.ReportElementAsync(homeNode, homeFile, config, new uSyncImportOptions())).Single();
        Assert.That(report.Change, Is.EqualTo(ChangeType.NoChange), report.Message);
    }

    [Test]
    public async Task DeletingAMappedItem_RemovesTheMapping()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        await contentTypeService.CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var home = contentService.Create("home", -1, contentType);
        contentService.Save(home);

        var keyMap = GetRequiredService<ISyncKeyMap>();
        var fileKey = Guid.NewGuid();
        keyMap.Add("Content", fileKey, home.Key);

        contentService.Delete(home);

        Assert.That(keyMap.TryGetLocal(fileKey, out _), Is.False);
    }

    [Test]
    public async Task Languages_AreNotMapped_AndReportNoChange()
    {
        var language = (await GetRequiredService<ILanguageService>().GetAllAsync()).First();

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<LanguageHandler>().Single();
        var config = handler.DefaultConfig.Clone();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };
        var fileService = GetRequiredService<ISyncFileService>();

        var file = (await handler.ExportAsync(language, folders, config)).Single(x => x.Success).FileName!;
        var node = await fileService.LoadXElementAsync(file);

        // the language file key is made from the iso code, so it never matches the site key.
        var fileKey = node.GetKey();
        Assert.That(fileKey, Is.Not.EqualTo(language.Key));

        // a mapping left over from before languages were left out is cleared by the scan.
        var keyMap = GetRequiredService<ISyncKeyMap>();
        keyMap.Add("Language", fileKey, language.Key);

        await handler.ScanKeyConflictsAsync([node]);

        Assert.That(keyMap.TryGetLocal(fileKey, out _), Is.False, "languages are not added to the key map");

        var report = (await handler.ReportElementAsync(node, file, config, new uSyncImportOptions())).Single();
        Assert.That(report.Change, Is.EqualTo(ChangeType.NoChange), report.Message);
    }

    private static XElement SwapKey(XElement node, Guid from, Guid to)
        => XElement.Parse(node.ToString(SaveOptions.DisableFormatting).Replace(from.ToString(), to.ToString()), LoadOptions.PreserveWhitespace);
}

/// <summary>
///  KeyConflict: Set (the default) - the same import as above sets the key
///  before Umbraco 18, and fails with a key-conflict error from 18 on.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class KeyConflictSetModeTests : UmbracoIntegrationTest
{
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
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

    private const string _root = "uSync/tests/keyconflictset";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    [Test]
    public async Task Import_ItemWithDifferentKey_SetsKeyOrReportsConflict()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        await contentTypeService.CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
        await contentTypeService.UpdateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var site = contentService.Create("site", -1, contentType);
        contentService.Save(site);
        var home = contentService.Create("home", site.Id, contentType);
        contentService.Save(home);

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();
        var config = handler.DefaultConfig.Clone();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };
        var fileService = GetRequiredService<ISyncFileService>();

        var homeFile = (await handler.ExportAsync(home, folders, config)).Single(x => x.Success).FileName!;
        var fileKey = Guid.NewGuid();
        var homeNode = XElement.Parse((await fileService.LoadXElementAsync(homeFile)).ToString(SaveOptions.DisableFormatting)
            .Replace(home.Key.ToString(), fileKey.ToString()), LoadOptions.PreserveWhitespace);

        Assert.That(GetRequiredService<ISyncKeyMap>().Enabled, Is.False);

        var actions = (await handler.ImportElementAsync(homeNode, homeFile, config, new uSyncImportOptions { Flags = SerializerFlags.Force })).ToList();

        if (SyncKeyHelper.KeysAreImmutable)
        {
            var failed = actions.Single(x => !x.Success);
            Assert.That(failed.Message, Does.Contain("Key conflict"));
            Assert.That(failed.Message, Does.Contain(Core.uSyncConstants.KeyConflictDocsUrl));
            Assert.That(contentService.GetById(home.Id)!.Key, Is.EqualTo(home.Key));
        }
        else
        {
            Assert.That(actions.All(x => x.Success), Is.True, string.Join(", ", actions.Select(x => x.Message)));
            Assert.That(contentService.GetById(home.Id)!.Key, Is.EqualTo(fileKey));
        }
    }
}
