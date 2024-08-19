using System;
using System.Text.Json.Serialization;
using System.Xml.Linq;

using Umbraco.Extensions;

using uSync.Core;

namespace uSync.BackOffice;

/// <summary>
///  object representing a file and its level
/// </summary>
public class OrderedNodeInfo
{
    /// <summary>
    ///  construct an OrderedNode
    /// </summary>
    public OrderedNodeInfo(string filename, XElement node)
    {
        FileName = filename;
        Node = node;
        Key = $"{node.Name.LocalName}_{node.GetKey()}".ToGuid();
        Alias = node.GetAlias();
        Path = string.Empty;
    }

    /// <summary>
    ///  set all the values of an ordered node. 
    /// </summary>
    [JsonConstructor]
    public OrderedNodeInfo(string filename, XElement node, int level, string path, bool isRoot)
        : this(filename, node)
    {
        Level = level;
        Path = path;
        IsRoot = isRoot;
    }

    /// <summary>
    ///  the key for the item.
    /// </summary>
    public Guid Key
    {
        get;
        [Obsolete("Setter will be removed v15")]
        set;
    }

    /// <summary>
    ///  umbraco alias of the item
    /// </summary>
    public string Alias { get;  }

    /// <summary>
    ///  relative path of the item (so same in all 'folders')
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///  level (e.g 0 is root) of file
    /// </summary>
    public int Level { get; }

    /// <summary>
    ///  path to the actual file.
    /// </summary>
    public string FileName
    {
        get;
        [Obsolete("Setter will be removed v15")]
        set;
    }

    /// <summary>
    ///  the xml for this item.
    /// </summary>
    public XElement Node
    {
        get;
        [Obsolete("Setter will be private from v15")]
        set;
    }

    /// <summary>
    ///  overwrites the node value for this ordered node element.
    /// </summary>
    /// <param name="node"></param>
    public void SetNode(XElement node)
        => Node = node;

    /// <summary>
    ///  is this element from a root folder ? 
    /// </summary>
    public bool IsRoot { get; }
}
