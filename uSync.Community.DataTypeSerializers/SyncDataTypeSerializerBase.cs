using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers;

public abstract class SyncDataTypeSerializerBase : ConfigurationSerializerBase
{
    protected readonly IEntityService entityService;

    public SyncDataTypeSerializerBase(IEntityService entityService)
    {
        this.entityService = entityService;
    }

    protected virtual string UdiToEntityPath(Udi udi)
    {
        if (udi != null && udi is GuidUdi guidUdi)
        {
            var item = entityService.Get(guidUdi.Guid);
            if (item != null)
            {
                var type = Umbraco.Cms.Core.Models.ObjectTypes.GetUdiType(item.NodeObjectType);
                return type + ":" + GetItemPath(item);
            }
        }
        return string.Empty;
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

    protected virtual Udi? PathToUdi(string entityPath)
    {
        if (entityPath.IndexOf(':') == -1) return null;

        var entityType = entityPath.Substring(0, entityPath.IndexOf(':'));
        var objectType = UdiEntityTypeHelper.ToUmbracoObjectType(entityType);

        var names = entityPath.Substring(entityPath.IndexOf(':') + 1).ToDelimitedList("/");

        int parentId = -1;

        IEntitySlim? next = null;

        foreach (var name in names)
        {
            next = FindItem(parentId, name, objectType);
            if (next == null) return null;

            parentId = next.Id;
        }

        if (next != null)
            return Udi.Create(entityType, next.Key);


        return null;
    }

    protected IEntitySlim? FindItem(int parentId, string name, UmbracoObjectTypes objectType)
    {
        var children = entityService.GetChildren(parentId, objectType);
        if (children.Any())
        {
            return children.FirstOrDefault(x => x.Name.InvariantEquals(name));
        }

        return null;
    }

}

public class MappedPathConfigBase<TObject>
{
    public TObject? Config { get; set; }

    public string? MappedPath { get; set; }
}
