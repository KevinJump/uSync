using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Cache;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Extensions;

namespace uSync.Tests.Cache;

/// <summary>
///  tests for the import state cache - the thing that lets a repeat import skip items it has
///  already confirmed match, without going near the database.
/// </summary>
/// <remarks>
///  these run against a real temp folder rather than a mocked file service, because the bits
///  most likely to break (does a hash survive being written to disk and read back, does an
///  invalidation survive a restart) are precisely the bits that involve real files.
/// </remarks>
[TestFixture]
internal class SyncStateCacheTests
{
    private string _tempFolder;
    private uSyncSettings _settings;
    private uSyncHandlerSetSettings _setSettings;
    private string _databaseStamp;
    private ISyncFileService _fileService;

    [SetUp]
    public void Setup()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "uSyncStateCacheTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);

        _settings = new uSyncSettings { CacheImportState = true };
        _setSettings = new uSyncHandlerSetSettings();
        _databaseStamp = Guid.NewGuid().ToString();

        var hostEnvironment = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(_tempFolder);
        _fileService = new SyncFileService(NullLogger<SyncFileService>.Instance, hostEnvironment.Object);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true);
        }
        catch
        {
            // a leftover temp folder is not worth failing a test run over.
        }
    }

    /// <summary>
    ///  build a cache pointed at this test's temp folder. call it more than once to simulate
    ///  a restart - the second instance sees whatever the first one left on disk.
    /// </summary>
    private SyncStateCache CreateCache()
    {
        var configService = new Mock<ISyncConfigService>();
        configService.SetupGet(x => x.Settings).Returns(() => _settings);
        configService.Setup(x => x.GetFolders()).Returns(() => _settings.Folders);
        configService.Setup(x => x.GetDefaultSetSettings()).Returns(() => _setSettings);

        var hostingEnvironment = new Mock<Umbraco.Cms.Core.Hosting.IHostingEnvironment>();
        hostingEnvironment.SetupGet(x => x.LocalTempPath).Returns(_tempFolder);

        var keyValueService = new Mock<IKeyValueService>();
        keyValueService.Setup(x => x.GetValue("uSync.StateCache.Id")).Returns(() => _databaseStamp);

        return new SyncStateCache(
            NullLogger<SyncStateCache>.Instance,
            configService.Object,
            _fileService,
            hostingEnvironment.Object,
            keyValueService.Object);
    }

    private static XElement MakeNode(string itemType, Guid key, string alias, string value = "a value")
        => XElement.Parse(
            $"<{itemType} Key=\"{key}\" Alias=\"{alias}\" Level=\"1\">" +
            $"  <Info><Name>{alias}</Name></Info>" +
            $"  <Value>{value}</Value>" +
            $"</{itemType}>");

    private static XElement MakeEmptyNode(Guid key, string alias)
        => XElement.Parse($"<Empty Key=\"{key}\" Alias=\"{alias}\" Change=\"Delete\" />");

    #region The hash has to survive a round trip through the file system

    // this is the test that matters most. warming the cache on export records the hash of the
    // node we serialized; the next import hashes the node it loaded back off disk. if those two
    // hashes do not agree the export warm-up silently does nothing, and nobody would notice
    // except that imports stay slow.
    [Test]
    public async Task Hash_OfSerializedNode_MatchesHashAfterSaveAndReload()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");
        var file = Path.Combine(_tempFolder, "round-trip.config");

        var hashBefore = await node.MakePlatformSafeHashAsync();

        await _fileService.SaveXElementAsync(node, file);
        var reloaded = await _fileService.LoadXElementAsync(file);

        var hashAfter = await reloaded.MakePlatformSafeHashAsync();

        Assert.That(hashAfter, Is.EqualTo(hashBefore));
    }

    #endregion

    #region Recording and looking up

    [Test]
    public async Task RecordedItem_IsKnownCurrent()
    {
        var cache = CreateCache();
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        await cache.RecordAsync(node);

        Assert.That(await cache.IsKnownCurrentAsync(node), Is.True);
    }

    [Test]
    public async Task ChangedFileContent_IsNotKnownCurrent()
    {
        var cache = CreateCache();
        var key = Guid.NewGuid();

        await cache.RecordAsync(MakeNode(uSyncConstants.Serialization.Content, key, "home"));

        var changed = MakeNode(uSyncConstants.Serialization.Content, key, "home", "a different value");

        Assert.That(await cache.IsKnownCurrentAsync(changed), Is.False);
    }

    [Test]
    public async Task UnknownItem_IsNotKnownCurrent()
    {
        var cache = CreateCache();
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        Assert.That(await cache.IsKnownCurrentAsync(node), Is.False);
    }

    [Test]
    public async Task WhenTurnedOff_NothingIsKnownCurrent()
    {
        var cache = CreateCache();
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");
        await cache.RecordAsync(node);

        _settings.CacheImportState = false;

        var isKnown = await cache.IsKnownCurrentAsync(node);

        Assert.Multiple(() =>
        {
            Assert.That(cache.IsEnabled, Is.False);
            Assert.That(isKnown, Is.False);
        });
    }

    // delete/rename/clean markers must never be cached: whether they are "current" depends on
    // whether the item still exists, and they are filed under the 'Empty' node name, so an
    // invalidation for the real item would never reach them.
    [Test]
    public async Task ActionMarkers_AreNeverCached()
    {
        var cache = CreateCache();
        var node = MakeEmptyNode(Guid.NewGuid(), "gone");

        await cache.RecordAsync(node);

        var isKnown = await cache.IsKnownCurrentAsync(node);

        Assert.Multiple(() =>
        {
            Assert.That(isKnown, Is.False);
            Assert.That(cache.Count, Is.Zero);
        });
    }

    [Test]
    public async Task NodeWithNoKey_IsNotCached()
    {
        var cache = CreateCache();
        var node = XElement.Parse("<Content Alias=\"no-key\" />");

        await cache.RecordAsync(node);

        Assert.That(cache.Count, Is.Zero);
    }

    #endregion

    #region Invalidation

    [Test]
    public async Task Invalidate_ForgetsThatItemOnly()
    {
        var cache = CreateCache();
        var goneKey = Guid.NewGuid();
        var stays = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "stays");
        var goes = MakeNode(uSyncConstants.Serialization.Content, goneKey, "goes");

        await cache.RecordAsync(stays);
        await cache.RecordAsync(goes);

        await cache.InvalidateAsync(uSyncConstants.Serialization.Content, goneKey);

        var goesIsKnown = await cache.IsKnownCurrentAsync(goes);
        var staysIsKnown = await cache.IsKnownCurrentAsync(stays);

        Assert.Multiple(() =>
        {
            Assert.That(goesIsKnown, Is.False);
            Assert.That(staysIsKnown, Is.True);
        });
    }

    // a move rewrites the path of every descendant, and paths are part of the serialized xml,
    // so a move has to drop the whole type - not just the item that moved.
    [Test]
    public async Task InvalidateType_ForgetsThatTypeButLeavesOthers()
    {
        var cache = CreateCache();
        var content = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "page");
        var media = MakeNode(uSyncConstants.Serialization.Media, Guid.NewGuid(), "image");

        await cache.RecordAsync(content);
        await cache.RecordAsync(media);

        await cache.InvalidateTypeAsync(uSyncConstants.Serialization.Content);

        var contentIsKnown = await cache.IsKnownCurrentAsync(content);
        var mediaIsKnown = await cache.IsKnownCurrentAsync(media);

        Assert.Multiple(() =>
        {
            Assert.That(contentIsKnown, Is.False);
            Assert.That(mediaIsKnown, Is.True);
        });
    }

    // renaming a doc type changes the serialized xml of every item that uses it, while leaving
    // those items' own rows - and notifications - untouched. so a doc type change has to clear
    // everything, even though we were only told about one item.
    [Test]
    public async Task Invalidate_ForATypeOthersEmbed_ForgetsEverything()
    {
        var cache = CreateCache();
        var content = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "page");
        var media = MakeNode(uSyncConstants.Serialization.Media, Guid.NewGuid(), "image");

        await cache.RecordAsync(content);
        await cache.RecordAsync(media);

        await cache.InvalidateAsync(uSyncConstants.Serialization.ContentType, Guid.NewGuid());

        var contentIsKnown = await cache.IsKnownCurrentAsync(content);
        var mediaIsKnown = await cache.IsKnownCurrentAsync(media);

        Assert.Multiple(() =>
        {
            Assert.That(contentIsKnown, Is.False);
            Assert.That(mediaIsKnown, Is.False);
            Assert.That(cache.Count, Is.Zero);
        });
    }

    [Test]
    public async Task InvalidateAll_ForgetsEverything()
    {
        var cache = CreateCache();
        await cache.RecordAsync(MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "one"));
        await cache.RecordAsync(MakeNode(uSyncConstants.Serialization.Media, Guid.NewGuid(), "two"));

        await cache.InvalidateAllAsync();

        Assert.That(cache.Count, Is.Zero);
    }

    // two different types can legitimately share a key, so entries are filed per type.
    [Test]
    public async Task Entries_AreScopedByItemType()
    {
        var cache = CreateCache();
        var sharedKey = Guid.NewGuid();
        var content = MakeNode(uSyncConstants.Serialization.Content, sharedKey, "page");
        var media = MakeNode(uSyncConstants.Serialization.Media, sharedKey, "image");

        await cache.RecordAsync(content);
        await cache.RecordAsync(media);

        await cache.InvalidateAsync(uSyncConstants.Serialization.Content, sharedKey);

        var contentIsKnown = await cache.IsKnownCurrentAsync(content);
        var mediaIsKnown = await cache.IsKnownCurrentAsync(media);

        Assert.Multiple(() =>
        {
            Assert.That(contentIsKnown, Is.False);
            Assert.That(mediaIsKnown, Is.True);
        });
    }

    #endregion

    #region Surviving a restart

    [Test]
    public async Task Entries_SurviveAReload()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        var second = CreateCache();
        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.True);
    }

    // the journal. an item saved after a run has finished is only removed from memory - the
    // copy in the file on disk is what the next restart trusts, so the invalidation has to be
    // written down as it happens.
    [Test]
    public async Task Invalidation_AfterTheFileIsWritten_SurvivesAReload()
    {
        var key = Guid.NewGuid();
        var node = MakeNode(uSyncConstants.Serialization.Content, key, "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        // ... run finishes, then someone edits the item.
        await first.InvalidateAsync(uSyncConstants.Serialization.Content, key);

        var second = CreateCache();
        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.False);
    }

    [Test]
    public async Task Invalidation_MadeBeforeAnythingIsLoaded_StillSurvivesAReload()
    {
        var key = Guid.NewGuid();
        var node = MakeNode(uSyncConstants.Serialization.Content, key, "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        // a fresh instance that has not loaded anything yet - which is where a save lands if
        // uSync has not run since the site started.
        var second = CreateCache();
        await second.InvalidateAsync(uSyncConstants.Serialization.Content, key);

        var third = CreateCache();
        await third.LoadAsync();

        Assert.That(await third.IsKnownCurrentAsync(node), Is.False);
    }

    [Test]
    public async Task ChangedSettings_ThrowTheCacheAway()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        // changing a setting changes what uSync writes out, so the hashes we recorded no
        // longer tell us anything.
        _settings.RootFolder = "uSync/somewhere-else/";

        var second = CreateCache();
        await second.LoadAsync();

        var isKnown = await second.IsKnownCurrentAsync(node);

        Assert.Multiple(() =>
        {
            Assert.That(isKnown, Is.False);
            Assert.That(second.Count, Is.Zero);
        });
    }

    [Test]
    public async Task ChangedHandlerSettings_ThrowTheCacheAway()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        _setSettings.Handlers["contentHandler"] = new HandlerSettings
        {
            Settings = new() { ["Cultures"] = "en-gb" }
        };

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        // handler settings feed into the serializer, so they change the xml we compare against.
        _setSettings.Handlers["contentHandler"].Settings["Cultures"] = "en-gb,fr-fr";

        var second = CreateCache();
        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.False);
    }

    // the identity has to come out the same on every start of the same site. dictionaries bound
    // from configuration enumerate in whatever order the provider hands them over, so if the
    // identity depended on that order the cache would silently never survive a restart - and the
    // only symptom would be "the feature does nothing".
    [Test]
    public async Task Identity_IsTheSame_WhenSettingsArriveInADifferentOrder()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        _setSettings.DisabledHandlers = ["zebra", "apple"];
        _setSettings.Handlers["zebraHandler"] = new HandlerSettings { Settings = new() { ["z"] = "1", ["a"] = "2" } };
        _setSettings.Handlers["appleHandler"] = new HandlerSettings { Settings = new() { ["m"] = "3" } };

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        // same settings, different insertion order - as a restart could easily produce.
        _setSettings = new uSyncHandlerSetSettings { DisabledHandlers = ["apple", "zebra"] };
        _setSettings.Handlers["appleHandler"] = new HandlerSettings { Settings = new() { ["m"] = "3" } };
        _setSettings.Handlers["zebraHandler"] = new HandlerSettings { Settings = new() { ["a"] = "2", ["z"] = "1" } };

        var second = CreateCache();
        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.True);
    }

    // restoring or pointing at a different database has to invalidate everything - the cache
    // describes a database, not the files.
    [Test]
    public async Task ADifferentDatabase_ThrowsTheCacheAway()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        _databaseStamp = Guid.NewGuid().ToString();

        var second = CreateCache();
        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.False);
    }

    // if we cannot get a stamp out of the database we cannot prove the cache applies to it,
    // so we must not use the cache at all.
    [Test]
    public async Task WithNoDatabaseStamp_TheCacheIsNotUsed()
    {
        var node = MakeNode(uSyncConstants.Serialization.Content, Guid.NewGuid(), "home");

        var first = CreateCache();
        await first.RecordAsync(node);
        await first.PersistAsync();

        var configService = new Mock<ISyncConfigService>();
        configService.SetupGet(x => x.Settings).Returns(_settings);
        configService.Setup(x => x.GetFolders()).Returns(_settings.Folders);
        configService.Setup(x => x.GetDefaultSetSettings()).Returns(_setSettings);

        var hostingEnvironment = new Mock<Umbraco.Cms.Core.Hosting.IHostingEnvironment>();
        hostingEnvironment.SetupGet(x => x.LocalTempPath).Returns(_tempFolder);

        var throwingKeyValueService = new Mock<IKeyValueService>();
        throwingKeyValueService.Setup(x => x.GetValue(It.IsAny<string>())).Throws(new InvalidOperationException("no database"));

        var second = new SyncStateCache(
            NullLogger<SyncStateCache>.Instance,
            configService.Object,
            _fileService,
            hostingEnvironment.Object,
            throwingKeyValueService.Object);

        await second.LoadAsync();

        Assert.That(await second.IsKnownCurrentAsync(node), Is.False);
    }

    #endregion
}
