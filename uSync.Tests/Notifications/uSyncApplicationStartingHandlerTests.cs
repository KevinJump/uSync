using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    private Task StartAsync()
    {
        var handler = ActivatorUtilities.CreateInstance<uSyncApplicationStartingHandler>(Services);
        return handler.HandleAsync(
            new UmbracoApplicationStartingNotification(RuntimeLevel.Run, false),
            CancellationToken.None);
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
