using System;
using System.Collections.Generic;

using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.Models;

/// <summary>
///  The view model for an action that has been performed
/// </summary>
public class uSyncActionView
{
    /// <summary>
    ///  Unique Id for Item
    /// </summary>
    public required Guid Key { get; set; }

    /// <summary>
    ///  Name of item
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///  Handler that performed the action
    /// </summary>
    public required string Handler { get; set; }

    /// <summary>
    ///  type (umbraco entity) for the item
    /// </summary>
    public required string ItemType { get; set; }

    /// <summary>
    ///  the type of change made
    /// </summary>
    public required ChangeType Change { get; set; }

    /// <summary>
    ///  was it successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///  like item details for the item
    /// </summary>
    public List<uSyncChange> Details { get; set; } = [];

    /// <summary>
    ///  any message that might have been generated during the action
    /// </summary>
    public string? Message { get; set; }
}
