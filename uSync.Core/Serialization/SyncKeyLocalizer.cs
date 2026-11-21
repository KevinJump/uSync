using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace uSync.Core.Serialization;

/// <summary>
///  swaps mapped keys in a uSync node for the keys of the items on this site.
/// </summary>
/// <remarks>
///  Guids are globally unique, so a string swap across the whole node is safe:
///  only guids in the map are replaced, everything else passes through.
///  Matches both "D" (dashed) and "N" (no dashes, as used in umb:// UDIs)
///  formats, in either case, and writes the local key back in the same format.
/// </remarks>
public static partial class SyncKeyLocalizer
{
    [GeneratedRegex(
        "(?<![0-9a-fA-F-])(?:[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-fA-F]{32})(?![0-9a-fA-F-])")]
    private static partial Regex GuidPattern();

    /// <summary>
    ///  return a copy of the node with mapped keys replaced,
    ///  or the node itself when nothing in it is mapped.
    /// </summary>
    public static XElement Localize(XElement node, IReadOnlyDictionary<Guid, Guid> map)
    {
        if (map.Count == 0) return node;

        var source = node.ToString(SaveOptions.DisableFormatting);
        var changed = false;

        var result = GuidPattern().Replace(source, match =>
        {
            if (!Guid.TryParse(match.ValueSpan, out var fileKey)
                || !map.TryGetValue(fileKey, out var localKey))
                return match.Value;

            changed = true;
            return Format(localKey, match.Value);
        });

        return changed
            ? XElement.Parse(result, LoadOptions.PreserveWhitespace)
            : node;
    }

    private static string Format(Guid key, string original)
    {
        var value = key.ToString(original.Length == 32 ? "N" : "D");
        return HasUpperCase(original) ? value.ToUpperInvariant() : value;
    }

    private static bool HasUpperCase(string value)
    {
        foreach (var c in value)
        {
            if (c is >= 'A' and <= 'F') return true;
        }
        return false;
    }
}
