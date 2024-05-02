using uSync.Core;
using uSync.Core.Models;

namespace uSync.Backoffice.Management.Api.Models;

public class uSyncActionView
{
    public required Guid Key { get;set; }
    public required string Name { get; set; }
    public required string ItemType { get; set; }
    public required ChangeType Change { get; set; }

    public bool Success { get; set; }

    public List<uSyncChange> Details { get; set; } = [];


}
