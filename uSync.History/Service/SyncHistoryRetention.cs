namespace uSync.History.Service;

/// <summary>
///  removes old history files based on the history retention settings.
/// </summary>
internal static class SyncHistoryRetention
{
    /// <summary>
    ///  prune the *.json files directly in the history folder.
    /// </summary>
    /// <remarks>
    ///  age and order come from each file's last write time (UTC),
    ///  a limit of 0 (or less) means no limit.
    /// </remarks>
    public static void Prune(string historyFolder, int maxAgeDays, int maxEntries, DateTime utcNow)
    {
        if (maxAgeDays <= 0 && maxEntries <= 0) return;
        if (Directory.Exists(historyFolder) is false) return;

        var files = new DirectoryInfo(historyFolder)
            .GetFiles("*.json", SearchOption.TopDirectoryOnly)
            .Where(x => x.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .ToList();

        if (maxAgeDays > 0)
        {
            var cutoff = utcNow.AddDays(-maxAgeDays);
            foreach (var file in files.Where(x => x.LastWriteTimeUtc < cutoff).ToList())
            {
                file.Delete();
                files.Remove(file);
            }
        }

        if (maxEntries > 0)
        {
            foreach (var file in files.Skip(maxEntries))
            {
                file.Delete();
            }
        }
    }
}
