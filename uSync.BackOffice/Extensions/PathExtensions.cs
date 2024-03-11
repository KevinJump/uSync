using System;
using System.IO;
using System.Linq;

namespace uSync.BackOffice.Extensions;

/// <summary>
///  extensions to manipulate folder path strings.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    ///  truncate a folder path to only show the last count paths..
    /// </summary>
    public static string TruncatePath(this string path, int count = 3, bool includeFile = false)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        var result = "";

        var fullPath = includeFile ? path : Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(fullPath)) return fullPath ?? string.Empty;

        var bits = fullPath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

        foreach (var item in bits.Reverse().Take(count))
        {
            if (Path.IsPathRooted(item)) continue;

            result = $"{Path.DirectorySeparatorChar}{item}{result}";
        }
      
        return result;
    }
}
