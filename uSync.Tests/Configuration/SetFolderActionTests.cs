using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Tests.Configuration;

/// <summary>
///  checks which folders the action service cleans and writes the version file to when a set has its own folders.
/// </summary>
[TestFixture]
public class SetFolderActionTests
{
    private static readonly string SiteRoot = Path.Combine(Path.GetTempPath(), "usync-set-folder-tests");

    private Mock<ISyncService> _syncService = null!;

    private SyncActionService CreateService(Dictionary<string, uSyncHandlerSetSettings> sets, params ISyncFolder[] registered)
    {
        var settings = new uSyncSettings { Folders = ["uSync/Root/", "uSync/v17/"] };

        var config = new SyncConfigService(
            new StubMonitor<uSyncSettings>(_ => settings),
            new StubMonitor<uSyncHandlerSetSettings>(name =>
                sets.TryGetValue(name ?? string.Empty, out var set) ? set : new uSyncHandlerSetSettings()),
            new SyncFolderCollection(() => registered));

        _syncService = new Mock<ISyncService>();

        var fileService = new Mock<ISyncFileService>();
        fileService.Setup(x => x.GetAbsPath(It.IsAny<string>()))
            .Returns<string>(p => Path.GetFullPath(Path.Combine(SiteRoot, p.TrimStart('/'))));

        var hosting = new Mock<Umbraco.Cms.Core.Hosting.IHostingEnvironment>();
        hosting.Setup(x => x.LocalTempPath).Returns(SiteRoot);

        return new SyncActionService(
            NullLogger<SyncActionService>.Instance,
            config,
            _syncService.Object,
            Mock.Of<ISyncHandlerFactory>(),
            fileService.Object,
            hosting.Object,
            Mock.Of<IWebHostEnvironment>(),
            BuildConfiguration(sets));
    }

    /// <summary>
    ///  the uSync:Sets section, so the action service can find the other sets.
    /// </summary>
    private static IConfiguration BuildConfiguration(Dictionary<string, uSyncHandlerSetSettings> sets)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(sets.Keys.ToDictionary(
                name => $"{uSync.BackOffice.uSync.Configuration.uSyncSetsConfigPrefix}{name}:IsSelectable",
                _ => (string?)"true"))
            .Build();

    private static SyncStartActionRequest CleanExport(string? set)
        => new() { HandlerAction = HandlerActions.Export, Clean = true, Set = set };

    [TestCase(null)]
    [TestCase("Default")]
    public async Task CleanExport_DefaultSetWithoutFolders_CleansWorkingFolder(string? set)
    {
        var service = CreateService(new() { ["Default"] = new() });

        await service.StartProcessAsync(CleanExport(set));

        _syncService.Verify(x => x.CleanExportFolder("uSync/v17/"), Times.Once);
    }

    [TestCase(null)]
    [TestCase("Default")]
    public async Task CleanExport_DefaultSetWithFolders_CleansSetFolder(string? set)
    {
        var service = CreateService(new() { ["Default"] = new() { Folders = ["uSync/default-set/"] } });

        await service.StartProcessAsync(CleanExport(set));

        _syncService.Verify(x => x.CleanExportFolder("uSync/default-set/"), Times.Once);
    }

    [TestCase("uSync/")]
    [TestCase("uSync/Root/")]
    public async Task CleanExport_SetFolderSharedWithGlobal_IsNotCleaned(string exportFolder)
    {
        var service = CreateService(new() { ["Set1"] = new() { Folders = ["uSync/sets/set1/", exportFolder] } });

        await service.StartProcessAsync(CleanExport("Set1"));

        _syncService.Verify(x => x.CleanExportFolder(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CleanExport_FolderContainingAnotherSetsFolder_IsNotCleaned()
    {
        var service = CreateService(new()
        {
            ["Default"] = new(),
            ["Set1"] = new() { Folders = ["uSync/v17/set1/"] }
        });

        await service.StartProcessAsync(CleanExport("Default"));

        _syncService.Verify(x => x.CleanExportFolder(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CleanExport_SetsSharingTheWorkingFolder_CleansIt()
    {
        var service = CreateService(new() { ["Default"] = new(), ["Set1"] = new() });

        await service.StartProcessAsync(CleanExport("Set1"));

        _syncService.Verify(x => x.CleanExportFolder("uSync/v17/"), Times.Once);
    }

    [Test]
    public async Task FinishExport_WritesVersionFileToSetFolder()
    {
        var service = CreateService(new() { ["Set1"] = new() { Folders = ["uSync/sets/set1/"] } });

        await service.FinishProcessAsync(new SyncFinalActionRequest
        {
            RequestId = Guid.NewGuid(),
            HandlerAction = HandlerActions.Export,
            ActionOptions = new SyncActionOptions { Set = "Set1" }
        });

        _syncService.Verify(x => x.FinishBulkProcessAsync(HandlerActions.Export, It.IsAny<string?>(), "uSync/sets/set1/", It.IsAny<IEnumerable<uSyncAction>>()), Times.Once);
    }

    [Test]
    public void SetFolders_IncludeRegisteredFolders()
    {
        var sets = new Dictionary<string, uSyncHandlerSetSettings>
        {
            ["Set1"] = new() { Folders = ["uSync/sets/set1/"] }
        };

        var config = new SyncConfigService(
            new StubMonitor<uSyncSettings>(_ => new uSyncSettings { Folders = ["uSync/v17/"] }),
            new StubMonitor<uSyncHandlerSetSettings>(name => sets.TryGetValue(name ?? string.Empty, out var s) ? s : new()),
            new SyncFolderCollection(() => [new TestFolder("uSync/Extra/", -100)]));

        Assert.That(config.GetFolders("Set1"), Is.EqualTo(new[] { "uSync/Extra/", "uSync/sets/set1/" }));
    }

    private sealed record TestFolder(string Path, int Weight) : ISyncFolder;

    private sealed class StubMonitor<T>(Func<string?, T> get) : IOptionsMonitor<T>
    {
        public T CurrentValue => get(Options.DefaultName);
        public T Get(string? name) => get(name);
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
