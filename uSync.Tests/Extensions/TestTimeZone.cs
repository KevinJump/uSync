using System;
using System.Reflection;

namespace uSync.Tests.Extensions;

/// <summary>
///  forces the process's local timezone (what DateTime.Now uses) for a test.
/// </summary>
/// <remarks>
///  there is no public API to override TimeZoneInfo.Local (and the TZ variable
///  doesn't work on Windows), so we set the runtime's cached local zone by
///  reflection. ClearCachedData() throws the cache away, restoring the real zone.
/// </remarks>
internal static class TestTimeZone
{
    private const BindingFlags _instance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void SetLocal(TimeSpan offset)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone($"uSyncTest{offset}", offset, $"Test {offset}", $"Test {offset}");

        TimeZoneInfo.ClearCachedData();
        var cachedData = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        var cachedDataType = cachedData.GetType();

        cachedDataType.GetField("_localTimeZone", _instance)!.SetValue(cachedData, zone);

        // on Windows DateTime.Now reads the offset from a per-year cache, not the local zone.
        var oneYearField = cachedDataType.GetField("_oneYearLocalFromUtc", _instance);
        if (oneYearField is not null)
        {
            var offsetAndRule = Activator.CreateInstance(oneYearField.FieldType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                [DateTime.UtcNow.Year, offset, null], null);
            oneYearField.SetValue(cachedData, offsetAndRule);
        }

        var actual = DateTime.Now - DateTime.UtcNow;
        if (Math.Abs((actual - offset).TotalMinutes) > 1)
            throw new InvalidOperationException($"Unable to force local timezone to {offset} (DateTime.Now is {actual} from UTC)");
    }

    public static void Restore() => TimeZoneInfo.ClearCachedData();
}
