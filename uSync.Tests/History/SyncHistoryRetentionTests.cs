using System;
using System.IO;

using NUnit.Framework;

using uSync.History.Service;

namespace uSync.Tests.History;

[TestFixture]
internal class SyncHistoryRetentionTests
{
    private static readonly DateTime _now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private string _root;
    private string _historyFolder;

    [SetUp]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "uSyncHistoryTests", Guid.NewGuid().ToString("N"));
        _historyFolder = Path.Combine(_root, "history");
        Directory.CreateDirectory(_historyFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private string CreateFile(string folder, string name, int daysOld)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        File.WriteAllText(path, "{}");
        File.SetLastWriteTimeUtc(path, _now.AddDays(-daysOld));
        return path;
    }

    [Test]
    public void NoLimits_DeletesNothing()
    {
        var oldFile = CreateFile(_historyFolder, "old.json", 100);
        var newFile = CreateFile(_historyFolder, "new.json", 0);

        SyncHistoryRetention.Prune(_historyFolder, 0, 0, _now);

        Assert.That(File.Exists(oldFile), Is.True);
        Assert.That(File.Exists(newFile), Is.True);
    }

    [Test]
    public void NegativeLimits_DeletesNothing()
    {
        var oldFile = CreateFile(_historyFolder, "old.json", 100);

        SyncHistoryRetention.Prune(_historyFolder, -1, -1, _now);

        Assert.That(File.Exists(oldFile), Is.True);
    }

    [Test]
    public void MaxAge_DeletesOlderFiles()
    {
        var old1 = CreateFile(_historyFolder, "a.json", 8);
        var old2 = CreateFile(_historyFolder, "b.json", 30);
        var new1 = CreateFile(_historyFolder, "c.json", 6);
        var new2 = CreateFile(_historyFolder, "d.json", 0);

        SyncHistoryRetention.Prune(_historyFolder, 7, 0, _now);

        Assert.That(File.Exists(old1), Is.False);
        Assert.That(File.Exists(old2), Is.False);
        Assert.That(File.Exists(new1), Is.True);
        Assert.That(File.Exists(new2), Is.True);
    }

    [Test]
    public void MaxEntries_KeepsNewestByTimestamp_NotName()
    {
        // names sort the opposite way to the timestamps.
        var f1 = CreateFile(_historyFolder, "a.json", 5);
        var f2 = CreateFile(_historyFolder, "b.json", 4);
        var f3 = CreateFile(_historyFolder, "c.json", 3);
        var f4 = CreateFile(_historyFolder, "d.json", 2);
        var f5 = CreateFile(_historyFolder, "e.json", 1);
        File.SetLastWriteTimeUtc(f1, _now.AddMinutes(-1));
        File.SetLastWriteTimeUtc(f2, _now.AddMinutes(-2));
        File.SetLastWriteTimeUtc(f3, _now.AddMinutes(-3));
        File.SetLastWriteTimeUtc(f4, _now.AddMinutes(-4));
        File.SetLastWriteTimeUtc(f5, _now.AddMinutes(-5));

        SyncHistoryRetention.Prune(_historyFolder, 0, 3, _now);

        Assert.That(File.Exists(f1), Is.True);
        Assert.That(File.Exists(f2), Is.True);
        Assert.That(File.Exists(f3), Is.True);
        Assert.That(File.Exists(f4), Is.False);
        Assert.That(File.Exists(f5), Is.False);
    }

    [Test]
    public void MaxAgeAndMaxEntries_AppliesBoth()
    {
        var tooOld = CreateFile(_historyFolder, "a.json", 10);
        var keep1 = CreateFile(_historyFolder, "b.json", 1);
        var keep2 = CreateFile(_historyFolder, "c.json", 2);
        var trimmed = CreateFile(_historyFolder, "d.json", 3);

        SyncHistoryRetention.Prune(_historyFolder, 7, 2, _now);

        Assert.That(File.Exists(tooOld), Is.False);
        Assert.That(File.Exists(keep1), Is.True);
        Assert.That(File.Exists(keep2), Is.True);
        Assert.That(File.Exists(trimmed), Is.False);
    }

    [Test]
    public void OnlyTopLevelJsonFilesAreDeleted()
    {
        var text = CreateFile(_historyFolder, "notes.txt", 100);
        var jsonBackup = CreateFile(_historyFolder, "old.json.bak", 100);
        var subFolderJson = CreateFile(Path.Combine(_historyFolder, "sub"), "old.json", 100);
        var siblingJson = CreateFile(Path.Combine(_root, "other"), "old.json", 100);
        var topLevel = CreateFile(_historyFolder, "old.json", 100);

        SyncHistoryRetention.Prune(_historyFolder, 1, 1, _now);

        Assert.That(File.Exists(text), Is.True);
        Assert.That(File.Exists(jsonBackup), Is.True);
        Assert.That(File.Exists(subFolderJson), Is.True);
        Assert.That(File.Exists(siblingJson), Is.True);
        Assert.That(File.Exists(topLevel), Is.False);
    }

    [Test]
    public void MissingFolder_DoesNotThrow()
    {
        var missing = Path.Combine(_root, "missing");

        Assert.DoesNotThrow(() => SyncHistoryRetention.Prune(missing, 7, 2, _now));
    }
}
