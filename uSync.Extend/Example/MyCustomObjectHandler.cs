using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Extend.Example;

/// <summary>
///  the attribute set it all up. this is how it's discovered and registered in uSync
/// </summary>
[SyncHandler("MyCustomObjectHandler",
    "My Custom Object Handler",
    "MyCustomObjects",
    uSyncConstants.Priorites.USYNC_RESERVED_UPPER + 100, // after all the core things
    Icon = "icon-files",
    EntityType = "myCustomObject")]
public class MyCustomObjectHandler : SyncObjectHandler<MyCustomObject>
{
    public override string Group => "My Custom Group";

    private readonly IMyCustomObjectService _myCustomObjectService;

    public MyCustomObjectHandler(
        ILogger<SyncHandlerRoot<MyCustomObject, MyCustomObject>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        Core.ISyncItemFactory itemFactory,
        IMyCustomObjectService myCustomObjectService) : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    {
        _myCustomObjectService = myCustomObjectService;
    }

    // here you would get all your items from your data source. 
    protected override async Task<IEnumerable<MyCustomObject>> GetAllItems()
        => await _myCustomObjectService.GetAllAsync();

    protected override string GetItemName(MyCustomObject item)
        => item.Name;
}