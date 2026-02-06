using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.Extend.Example;

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

