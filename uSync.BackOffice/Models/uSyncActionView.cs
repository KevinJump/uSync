using System;
using System.Collections.Generic;

using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.Models;

public class uSyncActionView
{
    public required Guid Key { get; set; }
    public required string Name { get; set; }
    public required string Handler { get; set; }
    public required string ItemType { get; set; }
    public required ChangeType Change { get; set; }
    public bool Success { get; set; }
    public List<uSyncChange> Details { get; set; } = [];

    public string? Message { get; set; }
}
