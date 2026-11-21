using System;
using System.Collections.Generic;
using System.IO;
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
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.Tests.Handlers;

/// <summary>
///  the lookups that read just the root element of a file (rather than the whole thing),
///  and the export tree walk that skips items it knows have no children.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class FileLookupTests : UmbracoIntegrationTest
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

    private const string _root = "uSync/tests/filelookups";

    private ISyncFileService FileService => GetRequiredService<ISyncFileService>();

    private ContentTypeHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentTypeHandler>().Single();

    // TryFindItemNodeAsync looks in the handler's configured folders.
    private string HandlerFolder
        => $"{GetRequiredService<ISyncConfigService>().GetFolders().Last().TrimEnd('/')}/{Handler.DefaultFolder}";

    [TearDown]
    public void RemoveExports()
    {
        FileService.DeleteFolder(_root);
        FileService.DeleteFolder(HandlerFolder);
    }

    private async Task<IContentType> CreateContentType(string alias, IContentType? parent = null)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = parent is null
            ? new ContentType(shortStringHelper, -1)
            : new ContentType(shortStringHelper, parent, alias);
        contentType.Alias = alias;
        contentType.Name = alias;
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        return contentType;
    }

    private string WriteFile(XElement node, string name)
    {
        var path = Path.Combine(FileService.GetAbsPath(_root), name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        node.Save(path);
        return path;
    }

    [Test]
    public async Task LoadRootElement_ReturnsAttributesWithoutChildren()
    {
        var key = Guid.NewGuid();
        var file = WriteFile(
            new XElement("ContentType",
                new XAttribute("Key", key),
                new XAttribute("Alias", "page"),
                new XElement("Info", new XElement("Name", "Page"))),
            "root.config");

        var root = await FileService.LoadRootElementAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(root.Name.LocalName, Is.EqualTo("ContentType"));
            Assert.That(root.GetKey(), Is.EqualTo(key));
            Assert.That(root.GetAlias(), Is.EqualTo("page"));
            Assert.That(root.HasElements, Is.False);
        });
    }

    [Test]
    public async Task LoadRootElement_ReadsEmptyMarkers()
    {
        var key = Guid.NewGuid();
        var file = WriteFile(XElementExtensions.MakeEmpty(key, SyncActionType.Rename, "old"), "rename.config");

        var root = await FileService.LoadRootElementAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(root.IsEmptyItem(), Is.True);
            Assert.That(root.GetEmptyAction(), Is.EqualTo(SyncActionType.Rename));
            Assert.That(root.GetKey(), Is.EqualTo(key));
        });
    }

    [Test]
    public void LoadRootElement_MissingFile_ThrowsFileNotFound()
        => Assert.ThrowsAsync<FileNotFoundException>(
            async () => await FileService.LoadRootElementAsync($"{_root}/missing/nothing.config"));

    [Test]
    public void LoadXElement_MissingFile_ThrowsFileNotFound()
        => Assert.ThrowsAsync<FileNotFoundException>(
            async () => await FileService.LoadXElementAsync($"{_root}/missing/nothing.config"));

    [Test]
    public async Task SaveXElement_OverwritesALongerFile()
    {
        var file = $"{_root}/overwrite.config";
        await FileService.SaveXElementAsync(new XElement("Item", new XElement("Value", new string('x', 1000))), file);
        await FileService.SaveXElementAsync(new XElement("Item", new XElement("Value", "short")), file);

        var node = await FileService.LoadXElementAsync(file);

        Assert.That(node.Element("Value")?.Value, Is.EqualTo("short"));
    }

    [Test]
    public async Task TryFindItemNode_FindsTheExportedItem()
    {
        var page = await CreateContentType("page");
        var other = await CreateContentType("other");
        await Handler.ExportAsync(page, [HandlerFolder], Handler.DefaultConfig);
        await Handler.ExportAsync(other, [HandlerFolder], Handler.DefaultConfig);

        var node = await Handler.TryFindItemNodeAsync(other.Key);

        Assert.Multiple(() =>
        {
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.GetKey(), Is.EqualTo(other.Key));
            Assert.That(node.GetAlias(), Is.EqualTo("other"));
            Assert.That(node.HasElements, Is.True, "should be the full item, not just the root element");
        });
    }

    [Test]
    public async Task TryFindItemNode_PrefersTheItemToItsCleanMarker()
    {
        var page = await CreateContentType("page");
        await Handler.ExportAsync(page, [HandlerFolder], Handler.DefaultConfig);

        var marker = XElementExtensions.MakeEmpty(page.Key, SyncActionType.Clean, "clean children");
        marker.Save(Path.Combine(FileService.GetAbsPath(HandlerFolder), "aaa_clean.config"));

        var node = await Handler.TryFindItemNodeAsync(page.Key);

        Assert.That(node?.IsEmptyItem(), Is.False);
    }

    [Test]
    public async Task TryFindItemNode_UnknownKey_ReturnsNull()
    {
        var page = await CreateContentType("page");
        await Handler.ExportAsync(page, [HandlerFolder], Handler.DefaultConfig);

        Assert.That(await Handler.TryFindItemNodeAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task Export_ExistingFileForSameItem_KeepsTheFileName()
    {
        var page = await CreateContentType("page");
        var folder = $"{_root}/{Guid.NewGuid():N}";

        var first = await Handler.ExportAsync(page, [folder], Handler.DefaultConfig);
        var second = await Handler.ExportAsync(page, [folder], Handler.DefaultConfig);

        Assert.That(second.Single().FileName, Is.EqualTo(first.Single().FileName));
    }

    [Test]
    public async Task ExportAll_ExportsChildTypes_BeneathLeafAndParentTypes()
    {
        var parent = await CreateContentType("parent");
        var child = await CreateContentType("child", parent);
        var grandchild = await CreateContentType("grandchild", child);
        var leaf = await CreateContentType("leaf");

        var actions = await Handler.ExportAllAsync([$"{_root}/{Guid.NewGuid():N}"], Handler.DefaultConfig, null);

        Assert.That(actions.Where(x => x.Success).Select(x => x.Key),
            Is.SupersetOf(new[] { parent.Key, child.Key, grandchild.Key, leaf.Key }));
    }
}
