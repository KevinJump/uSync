using System;
using System.Collections.Generic;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;

using uSync.Core;

namespace uSync.Tests.Extensions;

/// <summary>
///  schedule dates are stored in UTC, so the culture state must not depend on the server's local time.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ContentScheduleTimeZoneTests
{
    private const string _culture = "en-US";
    private TimeZoneInfo _originalZone = null!;

    [SetUp]
    public void SetUp() => _originalZone = TimeZoneInfo.Local;

    [TearDown]
    public void TearDown()
    {
        TestTimeZone.Restore();
        Assert.That(TimeZoneInfo.Local.Id, Is.EqualTo(_originalZone.Id));
        Assert.That(TimeZoneInfo.Local.BaseUtcOffset, Is.EqualTo(_originalZone.BaseUtcOffset));
    }

    private static IList<ContentSchedule> Schedule(ContentScheduleAction action, DateTime date)
        => [new ContentSchedule(_culture, date, action, false)];

    [Test]
    public void FutureRelease_LocalAheadOfUtc_IsPending()
    {
        TestTimeZone.SetLocal(TimeSpan.FromHours(11));

        var state = Schedule(ContentScheduleAction.Release, DateTime.UtcNow.AddHours(2))
            .CalculateCultureState(_culture, uSyncContentState.Published);

        Assert.That(state, Is.EqualTo(uSyncContentState.Saved));
    }

    [Test]
    public void PastRelease_LocalBehindUtc_IsPublished()
    {
        TestTimeZone.SetLocal(TimeSpan.FromHours(-5));

        var state = Schedule(ContentScheduleAction.Release, DateTime.UtcNow.AddHours(-2))
            .CalculateCultureState(_culture, uSyncContentState.Unpublished);

        Assert.That(state, Is.EqualTo(uSyncContentState.Published));
    }

    [Test]
    public void PastExpire_LocalBehindUtc_IsUnpublished()
    {
        TestTimeZone.SetLocal(TimeSpan.FromHours(-5));

        var state = Schedule(ContentScheduleAction.Expire, DateTime.UtcNow.AddHours(-2))
            .CalculateCultureState(_culture, uSyncContentState.Published);

        Assert.That(state, Is.EqualTo(uSyncContentState.Unpublished));
    }
}
