using System;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.DataTypes;

namespace uSync.Community.DataTypeSerializers;

public abstract class SyncDataTypeSerializerBase : ConfigurationSerializerBase
{
    protected readonly IEntityService entityService;

    public SyncDataTypeSerializerBase(IEntityService entityService)
    {
        this.entityService = entityService;
    }

    protected virtual bool TryUdiToEntityPath(Udi? udi, out string entityPath)
    {
        entityPath = string.Empty;

        if (udi is not GuidUdi guidUdi) return false;

        return TryGuidToEntityPath(guidUdi.Guid, out entityPath);
    }

    protected virtual bool TryGuidToEntityPath(Guid guid, out string entityPath)
    {
        entityPath = string.Empty;

        var item = entityService.Get(guid);
        if (item == null) return false;

        var type = ObjectTypes.GetUdiType(item.NodeObjectType);
        entityPath = type + ":" + GetItemPath(item);
        return true;
    }

    protected virtual string GetItemPath(IEntitySlim item)
    {
        var path = "";
        if (item.ParentId != -1)
        {
            var parent = entityService.Get(item.ParentId);
            if (parent != null)
                path += GetItemPath(parent);
        }

        return path + "/" + item.Name;
    }

    protected virtual bool TryPathToUdi(string entityPath, out Udi? udi)
    {
        udi = null;

        if (!entityPath.Contains(':')) return false;
        var entityType = entityPath.Substring(0, entityPath.IndexOf(':'));

        if (!TryPathToGuid(entityPath, out var key)) return false;

        udi = Udi.Create(entityType, key);
        return true;
    }

    protected virtual bool TryPathToGuid(string entityPath, out Guid guid)
    {
        guid = Guid.Empty;

        if (!entityPath.Contains(':')) return false;

        var entityType = entityPath.Substring(0, entityPath.IndexOf(':'));
        var objectType = UdiEntityTypeHelper.ToUmbracoObjectType(entityType);

        var names = entityPath.Substring(entityPath.IndexOf(':') + 1).ToDelimitedList("/");

        int parentId = -1;

        IEntitySlim? next = null;

        foreach (var name in names)
        {
            if (!TryFindItem(parentId, name, objectType, out next)) return false;

            parentId = next.Id;
        }

        if (next == null) return false;

        guid = next.Key;
        return true;
    }


    protected bool TryFindItem(int parentId, string name, UmbracoObjectTypes objectType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEntitySlim? item)
    {
        var children = entityService.GetChildren(parentId, objectType);

        item = children.FirstOrDefault(x => x.Name.InvariantEquals(name));
        return item != null;
    }

}

public class MappedPathConfigBase<TObject>
{
    public TObject? Config { get; set; }

    public string? MappedPath { get; set; }
}
