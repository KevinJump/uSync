using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.Tests.Handlers;

/// <summary>
///  a clean marker has the same key as the item it cleans, the
///  item must still be imported when the marker is in the folder.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CleanMarkerOrderTests : UmbracoIntegrationTest
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

    private const string _root = "uSync/tests/cleanmarkers";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private ContentTypeHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentTypeHandler>().Single();

    private IContentType CreateContentType(string alias)
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1) { Alias = alias, Name = alias };
        GetRequiredService<IContentTypeService>().Save(contentType);
        return contentType;
    }

    private void WriteCleanMarker(string folder, Guid key)
    {
        var marker = XElementExtensions.MakeEmpty(key, SyncActionType.Clean, "clean children");
        var path = GetRequiredService<ISyncFileService>().GetAbsPath(folder);
        marker.Save(Path.Combine(path, $"{key}_clean.config"));
    }

    [Test]
    public async Task FetchAllNodes_KeepsTheItem_WhenItHasACleanMarker()
    {
        var contentType = CreateContentType("page");
        var folder = $"{_root}/{Guid.NewGuid():N}/";

        await Handler.ExportAsync(contentType, [folder], Handler.DefaultConfig);
        WriteCleanMarker(folder, contentType.Key);

        var nodes = await Handler.FetchAllNodesAsync([folder]);

        Assert.That(nodes.Select(x => x.Node.GetEmptyAction()),
            Is.EqualTo(new[] { SyncActionType.None, SyncActionType.Clean }));
    }

    [Test]
    public async Task FetchAllNodes_PutsCleanMarkersAfterEveryItem()
    {
        var page = CreateContentType("page");
        var other = CreateContentType("other");
        var folder = $"{_root}/{Guid.NewGuid():N}/";

        await Handler.ExportAsync(page, [folder], Handler.DefaultConfig);
        await Handler.ExportAsync(other, [folder], Handler.DefaultConfig);
        WriteCleanMarker(folder, page.Key);
        WriteCleanMarker(folder, Guid.Empty);

        var nodes = await Handler.FetchAllNodesAsync([folder]);

        Assert.That(nodes.Select(x => x.Node.GetEmptyAction()), Is.EqualTo(new[]
        {
            SyncActionType.None, SyncActionType.None, SyncActionType.Clean, SyncActionType.Clean
        }));
    }

    [Test]
    public async Task Report_ShowsAChangedItem_WhenItHasACleanMarker()
    {
        var contentType = CreateContentType("page");
        var folder = $"{_root}/{Guid.NewGuid():N}/";

        await Handler.ExportAsync(contentType, [folder], Handler.DefaultConfig);
        WriteCleanMarker(folder, contentType.Key);

        contentType.Name = "page renamed";
        GetRequiredService<IContentTypeService>().Save(contentType);

        var actions = await Handler.ReportAsync([folder], Handler.DefaultConfig, null);

        Assert.That(actions.Where(x => x.Change == ChangeType.Update).Select(x => x.Key), Does.Contain(contentType.Key));
    }
}
