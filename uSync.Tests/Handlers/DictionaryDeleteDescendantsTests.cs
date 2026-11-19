using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
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
///  deleting a dictionary item deletes its descendants too, they need
///  delete markers or the next import puts them back at the root (#1248).
/// </summary>
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class DictionaryDeleteDescendantsTestsBase : UmbracoIntegrationTest
{
    private const string _root = "uSync/tests/dictionarydelete/";

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
            settings.RootFolder = _root;
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "All";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private DictionaryHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<DictionaryHandler>().Single();

    private IDictionaryItemService DictionaryItemService
        => GetRequiredService<IDictionaryItemService>();

    private string HandlerFolder => $"{_root}{Handler.DefaultFolder}";

    private async Task<IDictionaryItem> CreateItemAsync(string key, Guid? parentKey = null)
    {
        var attempt = await DictionaryItemService.CreateAsync(new DictionaryItem(parentKey, key), Constants.Security.SuperUserKey);
        Assert.That(attempt.Success, Is.True, $"Failed to create {key}");
        return attempt.Result;
    }

    private async Task DeleteItemAsync(IDictionaryItem item)
    {
        var attempt = await DictionaryItemService.DeleteAsync(item.Key, Constants.Security.SuperUserKey);
        Assert.That(attempt.Success, Is.True, $"Failed to delete {item.ItemKey}");
    }

    /// <summary>
    ///  load all the files in the dictionary folder, by the key of the item they are for.
    /// </summary>
    private Dictionary<Guid, XElement> LoadFiles()
    {
        var path = GetRequiredService<ISyncFileService>().GetAbsPath(HandlerFolder);
        return Directory.GetFiles(path, "*.config", SearchOption.AllDirectories)
            .Select(XElement.Load)
            .ToDictionary(x => x.GetKey(), x => x);
    }

    private static void AssertDeleteMarker(Dictionary<Guid, XElement> files, IDictionaryItem item)
    {
        Assert.That(files.ContainsKey(item.Key), Is.True, $"No file for {item.ItemKey}");
        Assert.That(files[item.Key].IsEmptyItem(), Is.True, $"{item.ItemKey} is not an empty node");
        Assert.That(files[item.Key].GetEmptyAction(), Is.EqualTo(SyncActionType.Delete), $"{item.ItemKey} is not a delete marker");
    }

    [Test]
    public async Task DeleteParent_ThenImport_DoesNotRecreateChild()
    {
        var parent = await CreateItemAsync("Parent");
        await CreateItemAsync("Parent.Child", parent.Key);

        await DeleteItemAsync(parent);

        await Handler.ImportAllAsync([HandlerFolder], Handler.DefaultConfig, new uSyncImportOptions());

        Assert.That(await DictionaryItemService.GetAsync("Parent.Child"), Is.Null);
        Assert.That(await DictionaryItemService.GetAsync("Parent"), Is.Null);
    }

    [Test]
    public async Task DeleteParent_WritesDeleteMarkerForChild()
    {
        var parent = await CreateItemAsync("Parent");
        var child = await CreateItemAsync("Parent.Child", parent.Key);
        var sibling = await CreateItemAsync("Sibling");

        await DeleteItemAsync(parent);

        var files = LoadFiles();
        AssertDeleteMarker(files, parent);
        AssertDeleteMarker(files, child);

        Assert.That(files[sibling.Key].IsEmptyItem(), Is.False);
        Assert.That(files[sibling.Key].Name.LocalName, Is.EqualTo("Dictionary"));
    }

    [Test]
    public async Task DeleteParent_WritesDeleteMarkerForGrandchild()
    {
        var parent = await CreateItemAsync("Parent");
        var child = await CreateItemAsync("Parent.Child", parent.Key);
        var grandchild = await CreateItemAsync("Parent.Child.Grand", child.Key);

        await DeleteItemAsync(parent);

        var files = LoadFiles();
        AssertDeleteMarker(files, parent);
        AssertDeleteMarker(files, child);
        AssertDeleteMarker(files, grandchild);
    }

    [Test]
    public async Task DeleteLeaf_WritesOneDeleteMarker()
    {
        var leaf = await CreateItemAsync("Leaf");
        var sibling = await CreateItemAsync("Sibling");

        await DeleteItemAsync(leaf);

        var files = LoadFiles();
        AssertDeleteMarker(files, leaf);
        Assert.That(files.Values.Count(x => x.IsEmptyItem()), Is.EqualTo(1));

        Assert.That(files[sibling.Key].IsEmptyItem(), Is.False);
        Assert.That(files[sibling.Key].Name.LocalName, Is.EqualTo("Dictionary"));
    }
}

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DictionaryDeleteDescendantsTests : DictionaryDeleteDescendantsTestsBase
{ }

/// <summary>
///  the same tests, with the handlers not using the flat folder structure.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class NonFlatDictionaryDeleteDescendantsTests : DictionaryDeleteDescendantsTestsBase
{
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["uSync:Sets:Default:HandlerDefaults:UseFlatStructure"] = "false"
        });
    }

    [Test]
    public void Handler_IsNotFlat()
        => Assert.That(GetRequiredService<SyncHandlerCollection>().OfType<DictionaryHandler>().Single().DefaultConfig.UseFlatStructure, Is.False);
}
