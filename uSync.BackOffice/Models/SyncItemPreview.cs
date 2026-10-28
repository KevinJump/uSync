using System;
using System.Collections.Generic;
using System.Xml.Linq;

using uSync.Core.Models;

namespace uSync.BackOffice.Models;

/// <summary>
///  the current and incoming versions of an item, used to preview an import before it happens.
/// </summary>
/// <remarks>
///  the incoming item is built in memory by the serializer (a dry run), it is never saved.
/// </remarks>
public class SyncItemPreview
{
    /// <summary>
    ///  key of the item
    /// </summary>
    public required Guid Key { get; set; }

    /// <summary>
    ///  alias of the handler that built the preview
    /// </summary>
    public required string Handler { get; set; }

    /// <summary>
    ///  item type (e.g IDataType)
    /// </summary>
    public required string ItemType { get; set; }

    /// <summary>
    ///  the uSync file the preview was built from
    /// </summary>
    public XElement? Node { get; set; }

    /// <summary>
    ///  the item as it currently is in Umbraco (null if it doesn't exist yet)
    /// </summary>
    public object? Current { get; set; }

    /// <summary>
    ///  the item as it would be after the import (null if the import would delete it)
    /// </summary>
    public object? Incoming { get; set; }

    /// <summary>
    ///  the import would delete or rename this item
    /// </summary>
    public bool IsDelete { get; set; }

    /// <summary>
    ///  did the dry run succeed
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///  any message from the dry run 
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///  changes reported by the serializer during the dry run.
    /// </summary>
    public IEnumerable<uSyncChange> Details { get; set; } = [];

    /// <summary>
    ///  items from the uSync files (not in Umbraco yet) that the incoming item uses, 
    ///  e.g. a new data type used by a new property.
    /// </summary>
    public IReadOnlyList<object> PendingItems { get; set; } = [];
}
