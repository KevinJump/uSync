using System;

using NUnit.Framework;

using uSync.BackOffice;
using uSync.BackOffice.Startup;
using uSync.Core;

namespace uSync.Tests.Startup;

[TestFixture]
public class SyncStartupImportStatusTests
{
    private static readonly DateTime _started = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _completed = _started.AddMinutes(1);

    [Test]
    public void FromResults_NoErrors_IsCompleted()
    {
        var status = SyncStartupImportStatus.FromResults(
            [
                uSyncAction.SetAction(true, "one", "Language", ChangeType.Import),
                uSyncAction.SetAction(true, "two", "Language", ChangeType.NoChange)
            ],
            _started, _completed);

        Assert.Multiple(() =>
        {
            Assert.That(status.State, Is.EqualTo(SyncStartupImportState.Completed));
            Assert.That(status.StartedUtc, Is.EqualTo(_started));
            Assert.That(status.CompletedUtc, Is.EqualTo(_completed));
            Assert.That(status.ItemsProcessed, Is.EqualTo(2));
            Assert.That(status.Changes, Is.EqualTo(1));
            Assert.That(status.Errors, Is.EqualTo(0));
        });
    }

    [Test]
    public void FromResults_WithFail_IsCompletedWithErrors()
    {
        var status = SyncStartupImportStatus.FromResults(
            [
                uSyncAction.SetAction(true, "one", "Language", ChangeType.Import),
                uSyncAction.Fail("two", "LanguageHandler", "Language", ChangeType.Fail, "failed", new Exception("failed"))
            ],
            _started, _completed);

        Assert.Multiple(() =>
        {
            Assert.That(status.State, Is.EqualTo(SyncStartupImportState.CompletedWithErrors));
            Assert.That(status.ItemsProcessed, Is.EqualTo(2));
            Assert.That(status.Errors, Is.EqualTo(1));
        });
    }

    [Test]
    public void FromResults_Empty_IsCompletedWithZeroCounts()
    {
        var status = SyncStartupImportStatus.FromResults([], _started, _completed);

        Assert.Multiple(() =>
        {
            Assert.That(status.State, Is.EqualTo(SyncStartupImportState.Completed));
            Assert.That(status.ItemsProcessed, Is.EqualTo(0));
            Assert.That(status.Changes, Is.EqualTo(0));
            Assert.That(status.Errors, Is.EqualTo(0));
        });
    }
}
