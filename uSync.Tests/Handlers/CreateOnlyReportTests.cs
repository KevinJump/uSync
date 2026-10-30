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
///  CreateOnly content that has changed in Umbraco since it was created
///  should not be reported as a pending change (#953).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CreateOnlyReportTests : UmbracoIntegrationTest
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

    private const string _root = "uSync/tests/createonly";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private ContentHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();

    private IContent CreateContent()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page" };
        GetRequiredService<IContentTypeService>().Save(contentType);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("home", -1, contentType);
        contentService.Save(content);
        return content;
    }

    private HandlerSettings CreateOnlyConfig()
    {
        var config = Handler.DefaultConfig.Clone();
        // appsettings.json values bind as strings.
        config.Settings[Core.uSyncConstants.DefaultSettings.CreateOnly] = "true";
        return config;
    }

    [Test]
    public async Task Report_CreateOnly_RenamedContent_IsNotAChange()
    {
        var content = CreateContent();
        var config = CreateOnlyConfig();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };

        await Handler.ExportAsync(content, folders, config);

        var contentService = GetRequiredService<IContentService>();
        content.Name = "home renamed";
        contentService.Save(content);

        var actions = (await Handler.ReportAsync(folders, config, null)).ToList();

        Assert.That(actions.Where(x => x.Change > ChangeType.NoChange).Select(x => $"{x.Change}: {x.Name} {x.Message}"),
            Is.Empty);
    }

    [Test]
    public async Task Report_RenamedContent_IsAChange()
    {
        var content = CreateContent();
        var config = Handler.DefaultConfig.Clone();
        var folders = new[] { $"{_root}/{Guid.NewGuid():N}/" };

        await Handler.ExportAsync(content, folders, config);

        content.Name = "home renamed";
        GetRequiredService<IContentService>().Save(content);

        var actions = (await Handler.ReportAsync(folders, config, null)).ToList();

        Assert.That(actions.Count(x => x.Change > ChangeType.NoChange), Is.EqualTo(1));
    }
}

/// <summary>
///  the same check, with CreateOnly set the way the #953 report configured it
///  (in appsettings, on the handler, over a HandlerDefaults value) and the report
///  run the way the dashboard runs it.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CreateOnlyReportConfigTests : UmbracoIntegrationTest
{
    private const string _root = "uSync/tests/createonly-config/";

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "1",
            ["Tests:Database:SchemaDatabaseCount"] = "1",
            ["Tests:Database:EmptyDatabasesCount"] = "0",

            ["uSync:Settings:RootFolder"] = _root,
            ["uSync:Settings:ImportAtStartup"] = "None",
            ["uSync:Settings:ExportOnSave"] = "None",
            ["uSync:Sets:Default:HandlerDefaults:UseFlatStructure"] = "false",
            ["uSync:Sets:Default:HandlerDefaults:Settings:CreateOnly"] = "false",
            ["uSync:Sets:Default:Handlers:ContentHandler:Settings:CreateOnly"] = "true",
            ["uSync:Sets:Default:Handlers:DictionaryHandler:Group"] = "Settings",
            ["uSync:Sets:Default:Handlers:DictionaryHandler:Settings:CreateOnly"] = "true",
        });
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        new ConvertLocalLinkComposer().Compose(builder);

        builder.AdduSync(settings =>
        {
            settings.ExportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    [Test]
    public async Task ReportHandler_CreateOnlyFromAppSettings_RenamedContent_IsNotAChange()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page" };
        GetRequiredService<IContentTypeService>().Save(contentType);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("home", -1, contentType);
        contentService.Save(content);

        var syncService = GetRequiredService<ISyncService>();
        var options = new uSyncImportOptions { HandlerSet = "Default", Folders = [_root] };

        var handler = GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();
        await handler.ExportAsync(content, [$"{_root}{handler.DefaultFolder}"], handler.DefaultConfig);

        content.Name = "home renamed";
        contentService.Save(content);

        var actions = (await syncService.ReportHandlerAsync(handler.Alias, options)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(actions.Select(x => x.Key), Does.Contain(content.Key), "the item was reported on");
            Assert.That(actions.Where(x => x.Change > ChangeType.NoChange).Select(x => $"{x.Change}: {x.Name} {x.Message}"),
                Is.Empty);
        });
    }
}
