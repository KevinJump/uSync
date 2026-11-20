using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Notifications;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;
using uSync.Core.Serialization.Serializers;

namespace uSync.Tests.Notifications;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class uSyncApplicationStartingHandlerTests : UmbracoIntegrationTest
{
    private string _relativeFolder;
    private string _exportFolder;

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
        _relativeFolder = $"uSync-startup-{Guid.NewGuid():N}";
        builder.AdduSync(settings =>
        {
            settings.Folders = [_relativeFolder];
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "None";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
            settings.ProcessingMode = SyncProcessingMode.Normal;
        });

        builder.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
            .Clear()
            .Add<LanguageHandler>()
            .Add<RelationTypeHandler>();
        builder.WithCollectionBuilder<SyncSerializerCollectionBuilder>()
            .Clear()
            .Add<LanguageSerializer>()
            .Add<RelationTypeSerializer>();
    }

    [SetUp]
    public async Task CreateLanguage()
    {
        _exportFolder = GetRequiredService<ISyncFileService>().GetAbsPath(_relativeFolder);
        AddOnTestTearDown(() =>
        {
            if (Directory.Exists(_exportFolder))
            {
                Directory.Delete(_exportFolder, true);
            }
        });

        var result = await GetRequiredService<ILanguageService>().CreateAsync(
            new Language("en-GB", "English (United Kingdom)"),
            Constants.Security.SuperUserKey);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task StartupExport_ExportsRequestedGroup_WhenExportOnSaveIsOff()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ExportAtStartup = "Settings";
        Directory.CreateDirectory(_exportFolder);

        await StartAsync();

        Assert.That(ExportedLanguageCodes(), Does.Contain("en-GB"));
    }

    [Test]
    public async Task StartupExport_DoesNotExportSaveGroup_WhenStartupRequestsAnotherGroup()
    {
        var relationKey = new Guid("2d21fba0-51f6-4c88-8c71-8c8b6c5073fb");
        GetRequiredService<IRelationService>().Save(
            new RelationType("Startup export relation", "startupExportRelation", false, null, null, false)
            {
                Key = relationKey
            });

        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ExportAtStartup = "Content";
        settings.ExportOnSave = "Settings";

        await StartAsync();

        Assert.That(ExportedLanguageCodes(), Does.Not.Contain("en-GB"));
        var exportedRelation = Directory.Exists(_exportFolder)
            ? Directory.GetFiles(_exportFolder, "*.config", SearchOption.AllDirectories)
                .Select(path => XDocument.Load(path).Root)
                .SingleOrDefault(node => node?.Name.LocalName == "RelationType"
                    && node.Attribute("Key")?.Value == relationKey.ToString())
            : null;
        Assert.That(exportedRelation?.Element("Info")?.Element("Name")?.Value,
            Is.EqualTo("Startup export relation"));
    }

    [Test]
    public async Task StartupExport_UsesSaveGroup_WhenStartupIsOffAndFoldersAreAbsent()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ExportOnSave = "Settings";

        await StartAsync();

        Assert.That(ExportedLanguageCodes(), Does.Contain("en-GB"));
    }

    [Test]
    public async Task StartupExport_DoesNotUseSaveGroup_WhenSyncFolderExists()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ExportAtStartup = "None";
        settings.ExportOnSave = "Settings";
        Directory.CreateDirectory(_exportFolder);

        await StartAsync();

        Assert.That(ExportedLanguageCodes(), Is.Empty);
    }

    [Test]
    public async Task StartupImport_KeepsOnceFile_WhenImportHasErrorsAndKeepIsOn()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ImportAtStartup = "Settings";
        settings.KeepOnceFileOnErrors = true;
        var workingFolder = CreateWorkingFolder(onceFile: true, stopFile: false);

        var syncService = MockStartupImport(FailedAction());
        await StartAsync(syncService.Object);

        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.OnceFile)), Is.True);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.StopFile)), Is.False);
    }

    [Test]
    public async Task StartupImport_ReplacesOnceFile_WhenImportHasErrorsAndKeepIsOff()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ImportAtStartup = "Settings";
        settings.KeepOnceFileOnErrors = false;
        var workingFolder = CreateWorkingFolder(onceFile: true, stopFile: false);

        var syncService = MockStartupImport(FailedAction());
        await StartAsync(syncService.Object);

        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.OnceFile)), Is.False);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.StopFile)), Is.True);
    }

    [Test]
    public async Task StartupImport_ReplacesOnceFile_WhenImportHasErrorsWithDefaultSettings()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ImportAtStartup = "Settings";
        var workingFolder = CreateWorkingFolder(onceFile: true, stopFile: false);

        Assert.That(settings.KeepOnceFileOnErrors, Is.False);

        var syncService = MockStartupImport(FailedAction());
        await StartAsync(syncService.Object);

        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.OnceFile)), Is.False);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.StopFile)), Is.True);
    }

    [Test]
    public async Task StartupImport_ReplacesOnceFile_WhenImportHasNoErrors()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ImportAtStartup = "Settings";
        var workingFolder = CreateWorkingFolder(onceFile: true, stopFile: false);

        var syncService = MockStartupImport(
            uSyncAction.SetAction(true, "en-GB", change: ChangeType.Import, filename: "languages/en-gb.config"));
        await StartAsync(syncService.Object);

        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.OnceFile)), Is.False);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.StopFile)), Is.True);
    }

    [Test]
    public async Task StartupImport_IsBlocked_WhenStopFileExistsWithoutOnceFile()
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        settings.ImportAtStartup = "Settings";
        var workingFolder = CreateWorkingFolder(onceFile: false, stopFile: true);

        var syncService = MockStartupImport(FailedAction());
        await StartAsync(syncService.Object);

        syncService.Verify(x => x.StartupImportAsync(
            It.IsAny<string[]>(), It.IsAny<bool>(), It.IsAny<SyncHandlerOptions>(), It.IsAny<uSyncCallbacks>()),
            Times.Never);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.OnceFile)), Is.False);
        Assert.That(System.IO.File.Exists(Path.Combine(workingFolder, settings.StopFile)), Is.True);
    }

    [Test]
    public void DescribeErrors_ListsAtMostTenItems_AndCountsTheRest()
    {
        var actions = Enumerable.Range(1, 12)
            .Select(x => uSyncAction.SetAction(false, $"item{x}", change: ChangeType.Fail, filename: x == 1 ? null : $"file{x}.config"))
            .Append(uSyncAction.SetAction(true, "ok", change: ChangeType.Import))
            .ToList();

        var description = actions.DescribeErrors();

        Assert.That(description, Does.StartWith("item1 ("));
        Assert.That(description, Does.Contain("file10.config"));
        Assert.That(description, Does.Not.Contain("file11.config"));
        Assert.That(description, Does.Not.Contain("ok"));
        Assert.That(description, Does.EndWith("(and 2 more)"));
    }

    private Task StartAsync(params object[] parameters)
    {
        var handler = ActivatorUtilities.CreateInstance<uSyncApplicationStartingHandler>(Services, parameters);
        return handler.HandleAsync(
            new UmbracoApplicationStartingNotification(RuntimeLevel.Run, false),
            CancellationToken.None);
    }

    private static uSyncAction FailedAction()
        => uSyncAction.SetAction(false, "broken", change: ChangeType.Fail, message: "failed", filename: "languages/broken.config");

    private static Mock<ISyncService> MockStartupImport(params uSyncAction[] results)
    {
        var syncService = new Mock<ISyncService>();
        syncService
            .Setup(x => x.StartupImportAsync(It.IsAny<string[]>(), It.IsAny<bool>(), It.IsAny<SyncHandlerOptions>(), It.IsAny<uSyncCallbacks>()))
            .ReturnsAsync(results);
        return syncService;
    }

    private string CreateWorkingFolder(bool onceFile, bool stopFile)
    {
        var settings = GetRequiredService<ISyncConfigService>().Settings;
        var workingFolder = GetRequiredService<ISyncFileService>()
            .GetAbsPath(GetRequiredService<ISyncConfigService>().GetWorkingFolder());
        Directory.CreateDirectory(workingFolder);

        if (onceFile) System.IO.File.WriteAllText(Path.Combine(workingFolder, settings.OnceFile), "once");
        if (stopFile) System.IO.File.WriteAllText(Path.Combine(workingFolder, settings.StopFile), "stop");

        return workingFolder;
    }

    private string[] ExportedLanguageCodes()
    {
        if (!Directory.Exists(_exportFolder))
        {
            return [];
        }

        return Directory.GetFiles(_exportFolder, "*.config", SearchOption.AllDirectories)
            .Select(path => XDocument.Load(path).Root)
            .Where(node => node?.Name.LocalName == "Language")
            .Select(node => node.Element("IsoCode")?.Value)
            .ToArray();
    }
}
