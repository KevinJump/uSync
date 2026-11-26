using System.Xml;
using System.Xml.Linq;

namespace uSync.Files;

/// <summary>
///  a uSync .config file that loaded as XML.
/// </summary>
public sealed class SyncFileItem
{
    public required string FilePath { get; init; }

    /// <summary>
    ///  path relative to the uSync folder, with forward slashes (ContentTypes/article.config)
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    ///  the top level folder the file is in (ContentTypes, DataTypes...), which
    ///  is what uSync uses to decide the type (handler) of an item.
    /// </summary>
    public required string TypeFolder { get; init; }

    public required XElement Node { get; init; }

    public string ElementName => Node.Name.LocalName;

    public Guid Key { get; init; }

    public string Alias { get; init; } = string.Empty;

    /// <summary>
    ///  an &lt;Empty&gt; file - uSync's marker for a delete, rename or clean.
    /// </summary>
    public bool IsEmpty => ElementName == "Empty";

    /// <summary>
    ///  the Change attribute of an &lt;Empty&gt; file (Delete, Rename, Clean)
    /// </summary>
    public string? Change => Node.Attribute("Change")?.Value;

    public bool IsDelete => IsEmpty && string.Equals(Change, "Delete", StringComparison.OrdinalIgnoreCase);

    public int Line => GetLine(Node);

    public static SyncFileItem Create(XElement node, string filePath, string relativePath, string typeFolder)
        => new()
        {
            FilePath = filePath,
            RelativePath = relativePath,
            TypeFolder = typeFolder,
            Node = node,
            Key = Guid.TryParse(node.Attribute("Key")?.Value, out var key) ? key : Guid.Empty,
            Alias = node.Attribute("Alias")?.Value ?? string.Empty,
        };

    internal static int GetLine(XObject node)
        => node is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 1;
}
