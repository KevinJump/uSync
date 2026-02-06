namespace uSync.Extend.Example;

/// <summary>
///  interface for the service, you don't need to have an interface
///  but in this example we are using one. it makes testing eaiser. 
/// </summary>
public interface IMyCustomObjectService
{
    Task<IEnumerable<MyCustomObject>> GetAllAsync();
    Task<MyCustomObject?> FindByKeyAsync(Guid key);
    Task<MyCustomObject?> FindByAliasAsync(string alias);
    Task SaveAsync(MyCustomObject item);
    Task DeleteAsync(MyCustomObject item);
}


/// <summary>
///  a exmaple service (you probibly already have one?) 
///  that can be used to answer the basic data access needs of the handler and serializer.
/// </summary>
internal class MyCustomObjectService : IMyCustomObjectService
{
    List<MyCustomObject> data =
    [
        new() { Key = Guid.Parse("b2885bf2-7780-40ad-b0c8-09473fd53712"), Alias = "item1", Name = "Item 1", Value = 1, DontShowThis = "Hidden 1" },
        new() { Key = Guid.Parse("db31860f-4ccb-43bb-9cb6-8701e3549163"), Alias = "item2", Name = "Item 2", Value = 2, DontShowThis = "Hidden 2" },
        new() { Key = Guid.Parse("ac417ecf-d270-4ce3-9816-0bbb1a709163"), Alias = "item3", Name = "Item 3", Value = 3, DontShowThis = "Hidden 3" },
    ];

    public Task DeleteAsync(MyCustomObject item) { 
        data.Remove(item);
        return Task.CompletedTask;
    }

    public Task<MyCustomObject?> FindByAliasAsync(string alias)
    {
        var item = data.FirstOrDefault(x => x.Alias == alias);
        return Task.FromResult(item);
    }

    public Task<MyCustomObject?> FindByKeyAsync(Guid key)
    {
        var item = data.FirstOrDefault(x => x.Key == key);
        return Task.FromResult(item);
    }

    public Task<IEnumerable<MyCustomObject>> GetAllAsync()
    {
        return Task.FromResult(data.AsEnumerable());
    }

    public Task SaveAsync(MyCustomObject item)
    {
        data.Add(item);
        return Task.CompletedTask;
    }
}
