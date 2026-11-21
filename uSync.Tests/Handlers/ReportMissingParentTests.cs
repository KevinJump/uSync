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
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.Tests.Handlers;

/// <summary>
///  an item whose parent isn't in Umbraco, but is in the same import, reports as a create.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ReportMissingParentTests : UmbracoIntegrationTest
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

    private const string _root = "uSync/tests/missingparents";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private ContentHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();

    [Test]
    public async Task Report_ChildOfAParentInTheImport_IsACreate()
    {
        var contentType = new ContentType(GetRequiredService<IShortStringHelper>(), -1)
        {
            Alias = "page",
            Name = "Page",
            AllowedAsRoot = true
        };
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var parent = contentService.Create("parent", -1, contentType);
        contentService.Save(parent);
        var child = contentService.Create("child", parent.Id, contentType);
        contentService.Save(child);

        var folder = $"{_root}/{Guid.NewGuid():N}";
        await Handler.ExportAllAsync([folder], Handler.DefaultConfig, null);

        // now neither item is in Umbraco, only in the export.
        contentService.Delete(parent);

        var actions = (await Handler.ReportAsync([folder], Handler.DefaultConfig, null)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(actions.Single(x => x.Key == parent.Key).Change, Is.EqualTo(ChangeType.Create));
            Assert.That(actions.Single(x => x.Key == child.Key).Change, Is.EqualTo(ChangeType.Create));
        });
    }
}
