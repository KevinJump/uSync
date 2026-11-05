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
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.Tests.Handlers;

/// <summary>
///  an item that fails to import is named after the item, not its file (#1200).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class FailedImportNameTests : UmbracoIntegrationTest
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

    private const string _root = "uSync/tests/failedimportname";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    [Test]
    public async Task Import_MissingContentType_FailIsNamedAfterItem()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentTypeService = GetRequiredService<IContentTypeService>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page" };
        contentTypeService.Save(contentType);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("home", -1, contentType);
        contentService.Save(content);

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();
        var config = handler.DefaultConfig.Clone();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };

        var exported = (await handler.ExportAsync(content, folders, config)).Single(x => x.Success);
        var node = await GetRequiredService<ISyncFileService>().LoadXElementAsync(exported.FileName!);

        // removing the doctype removes the content too, so the import has nothing to match.
        contentTypeService.Delete(contentType);

        var actions = (await handler.ImportElementAsync(node, exported.FileName!, config, new uSyncImportOptions())).ToList();
        var failed = actions.Single(x => !x.Success);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Change, Is.EqualTo(ChangeType.Fail));
            Assert.That(failed.Name, Is.EqualTo("home"));
            Assert.That(failed.Key, Is.EqualTo(content.Key));
            Assert.That(failed.FileName, Is.Not.Empty);
        });
    }
}
