namespace uSync.Files;

/// <summary>
///  finds git merge conflict markers in raw file text.
/// </summary>
/// <remarks>
///  this works on the raw text not the XML, because markers inside a CDATA
///  block (block list json, rich text values) still parse as valid XML.
/// </remarks>
public static class ConflictMarkerScanner
{
    /// <summary>
    ///  a conflict found in a file.
    /// </summary>
    /// <param name="StartLine">line of the first marker</param>
    /// <param name="EndLine">line of the last marker in the block</param>
    public readonly record struct ConflictBlock(int StartLine, int EndLine);

    public static IReadOnlyList<ConflictBlock> Scan(string text)
    {
        var blocks = new List<ConflictBlock>();
        int? start = null;
        var last = 0;

        var lines = text.Split('\n');
        for (var n = 0; n < lines.Length; n++)
        {
            var line = lines[n].TrimEnd('\r');
            var lineNumber = n + 1;

            if (line.StartsWith("<<<<<<<", StringComparison.Ordinal))
            {
                if (start is not null) blocks.Add(new ConflictBlock(start.Value, last));
                start = lineNumber;
                last = lineNumber;
            }
            else if (line.StartsWith(">>>>>>>", StringComparison.Ordinal))
            {
                blocks.Add(new ConflictBlock(start ?? lineNumber, lineNumber));
                start = null;
            }
            else if (line == "=======" || line.StartsWith("|||||||", StringComparison.Ordinal))
            {
                if (start is null) start = lineNumber;
                last = lineNumber;
            }
        }

        if (start is not null) blocks.Add(new ConflictBlock(start.Value, last));

        return blocks;
    }
}
