# uSync.Extend.
Helper classes to make it easy(er) to extend uSync.

uSync.Extend contains some helper classes and implimentations of ISyncHandlers and ISyncSerializers
that mean you can extend the functionality of uSync to include your own custom objects and data without
having to write the whole thing from scratch.

The extend package contains two base classes that simplfiy Handler and serializer creation for uSync.

### SyncObjectHandler
In uSync a handler is the thing that controlls the Input/Output of data to and from the file system.
its the thing that is called when an item is saved or when uSync exports or imports anything. 

The SyncObjectHandler is a base class that you can inherit from to create your own handlers.
It provides some basic functionality and structure to make it easier to create your own handlers.

Your own handler then only needs to provide the basic info on how to get and save your items.

```cs
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
```

### SyncObjectSerializer
The SyncObjectSerializer is a base class that you can inherit from to create your own serializers.

```cs
[SyncSerializer("c61e5987-020b-4ba8-899f-957d63443ac1", // uniqe id need to be a GUID
    "My Custom Object Serializer",  // name
    "MyCustomObject")] // the object type (node name in the xml)
public class MyCustomObjectSerializer : SyncObjectSerializer<MyCustomObject>
{
    private readonly IMyCustomObjectService _service;

    public MyCustomObjectSerializer(
        ILogger<SyncSerializerRoot<MyCustomObject>> logger,
        IMyCustomObjectService service) : base(logger)
    {
        _service = service;
    }
    public override MyCustomObject CreateItem(XElement node)
    {
        return new MyCustomObject
        {
            Key = node.GetKey(),
            Alias = node.GetAlias(),
        };
    }

    public override async Task DeleteItemAsync(MyCustomObject item)
        => await _service.DeleteAsync(item);

    public override async Task<MyCustomObject?> FindItemAsync(Guid key)
        => await _service.FindByKeyAsync(key);

    public override async Task<MyCustomObject?> FindItemAsync(string alias)
        => await _service.FindByAliasAsync(alias);

    public override string ItemAlias(MyCustomObject item) => item.Alias;

    public override Guid ItemKey(MyCustomObject item) => item.Key;

    public override Task SaveItemAsync(MyCustomObject item)
        => _service.SaveAsync(item);
}
```
