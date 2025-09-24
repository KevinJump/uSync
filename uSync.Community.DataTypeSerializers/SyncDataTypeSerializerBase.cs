using System;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers
{
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
                return GuidToEntityPath(guidUdi.Guid);
            return string.Empty;
        }

        protected string GuidToEntityPath(Guid guid)
        {
            var item = entityService.Get(guid);
            if (item is not null)
            {
                var type = Umbraco.Cms.Core.Models.ObjectTypes.GetUdiType(item.NodeObjectType);
                return type + ":" + GetItemPath(item);
            }
            return string.Empty;
        }

        protected virtual string GetItemPath(IEntitySlim item)
        {
            var path = "";
            if (item.ParentId != -1)
            {
                var parent = entityService.Get(item.ParentId);
                if (parent is not null)
                    path += GetItemPath(parent);
            }

            return path + "/" + item.Name;
        }

        protected virtual Udi? PathToUdi(string entityPath)
        {
            var key = PathToGuid(entityPath);
            if (key is null) return null;

            var entityType = entityPath.Substring(0, entityPath.IndexOf(':'));
            return Udi.Create(entityType, key.Value);
        }

        protected virtual Guid? PathToGuid(string entityPath)
        {
            if (!entityPath.Contains(':')) return null;

            var entityType = entityPath.Substring(0, entityPath.IndexOf(':'));
            var objectType = UdiEntityTypeHelper.ToUmbracoObjectType(entityType);

            var names = entityPath.Substring(entityPath.IndexOf(':') + 1).ToDelimitedList("/");

            int parentId = -1;
            IEntitySlim? next = null;

            foreach (var name in names)
            {
                next = FindItem(parentId, name, objectType);
                if (next is null) return null;

                parentId = next.Id;
            }

            return next?.Key;
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
        public required TObject Config { get; set; }

        public string? MappedPath { get; set; }

        public string? MappedRoot { get; set; }
    }

}
