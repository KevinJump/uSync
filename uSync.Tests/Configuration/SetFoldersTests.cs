using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

using NUnit.Framework;

using uSync.BackOffice.Configuration;

namespace uSync.Tests.Configuration;

[TestFixture]
public class SetFoldersTests
{
    private static SyncConfigService CreateService(
        uSyncSettings settings, Dictionary<string, uSyncHandlerSetSettings> sets)
        => new(
            new StubMonitor<uSyncSettings>(_ => settings),
            new StubMonitor<uSyncHandlerSetSettings>(name =>
                sets.TryGetValue(name ?? string.Empty, out var set) ? set : new uSyncHandlerSetSettings()),
            new SyncFolderCollection(() => []));

    private static uSyncSettings GlobalSettings() => new()
    {
        Folders = ["uSync/Root/", "uSync/v17/"]
    };

    [Test]
    public void SetWithoutFolders_UsesGlobalFolders()
    {
        var service = CreateService(GlobalSettings(), new() { ["Default"] = new() });

        Assert.That(service.GetFolders("Default"), Is.EqualTo(new[] { "uSync/Root/", "uSync/v17/" }));
    }

    [Test]
    public void UnknownOrEmptySet_UsesGlobalFolders()
    {
        var service = CreateService(GlobalSettings(), []);

        Assert.That(service.GetFolders("Missing"), Is.EqualTo(service.GetFolders()));
        Assert.That(service.GetFolders((string?)null), Is.EqualTo(service.GetFolders()));
        Assert.That(service.GetFolders(""), Is.EqualTo(service.GetFolders()));
    }

    [Test]
    public void SetWithFolders_ReplacesGlobalFolders_Normalised()
    {
        var service = CreateService(GlobalSettings(), new()
        {
            ["Set1"] = new() { Folders = ["/uSync/sets/set1", " ", "uSync/sets/shared/"] }
        });

        Assert.That(service.GetFolders("Set1"),
            Is.EqualTo(new[] { "uSync/sets/set1/", "uSync/sets/shared/" }));
    }

    [Test]
    public void SetFolders_IgnoredOutsideNormalFolderMode()
    {
        var settings = GlobalSettings();
        settings.FolderMode = SyncFolderMode.Root;

        var service = CreateService(settings, new()
        {
            ["Set1"] = new() { Folders = ["uSync/sets/set1/"] }
        });

        Assert.That(service.GetFolders("Set1"), Is.EqualTo(service.GetFolders()));
    }

    [Test]
    public void InterfaceDefault_FallsBackToGlobalFolders()
    {
        ISyncConfigService service = new LegacyConfigService();
        Assert.That(service.GetFolders("Set1"), Is.EqualTo(new[] { "legacy/" }));
    }

    /// <summary>
    ///  an implementation written before set folders existed - must still compile and work.
    /// </summary>
    private sealed class LegacyConfigService : ISyncConfigService
    {
        public uSyncSettings Settings { get; } = new();
        public string[] GetFolders() => ["legacy/"];
        public uSyncHandlerSetSettings GetDefaultSetSettings() => new();
        public uSyncHandlerSetSettings GetSetSettings(string setName) => new();
        public string GetWorkingFolder() => "legacy/";
    }

    private sealed class StubMonitor<T>(Func<string?, T> get) : IOptionsMonitor<T>
    {
        public T CurrentValue => get(Options.DefaultName);
        public T Get(string? name) => get(name);
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
