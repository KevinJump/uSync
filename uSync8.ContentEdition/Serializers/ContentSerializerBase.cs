using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    public abstract class ContentSerializerBase<TObject> : SyncTreeSerializerBase<TObject>
        where TObject : IContentBase
    {
        public ContentSerializerBase(IEntityService entityService) 
            : base(entityService)
        {
        }

        protected virtual XElement InitializeNode(TObject item, string typeName)
        {
            var node = new XElement(typeName,
                new XAttribute("Key", item.Key),
                new XAttribute("Level", item.Level));

            var parentKey = Guid.Empty;
            if (item.ParentId != -1)
            {
                var parent = GetItem(item.ParentId);
                if (parent != null)
                    parentKey = parent.Key;
            }

            node.Add(new XAttribute("Parent", parentKey));
            node.Add(new XAttribute("Path", GetItemPath(item)));

            return node;
        }

        // these are the functions using the simple 'getItem(alias)' 
        // that we cannot use for content/media trees.
        protected override TObject FindOrCreate(XElement node)
        {
            TObject item = GetItem(node);
            if (item != null) return item;

            var alias = node.GetAlias();

            var parentKey = node.Attribute("Parent").ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty)
            {
                item = GetItem(alias, parentKey);
                if (item != null) return item;
            }

            // create
            var parent = default(TObject);

            if (parentKey != Guid.Empty) {
                parent = GetItem(parentKey);
            }

            var contentTypeAlias = node.Name.LocalName;

            return CreateItem(alias, parent, null, contentTypeAlias);
        }

        protected override string GetItemBaseType(XElement node)
            => node.Name.LocalName;

        public override TObject GetItem(XElement node)
        {
            var (key, alias) = FindKeyAndAlias(node);
            if (key != Guid.Empty)
            {
                var item = GetItem(key);
                if (item != null) return item;
            }

            // else by level 
            var parentKey = node.Attribute("Parent").ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty)
            {
                var item = GetItem(alias, parentKey);
                if (item != null)
                    return item;
            }

            // if we get here, we could try for parent alias, alias ??
            // (really we would need full path e.g home/blog/2019/posts/)
            return default(TObject);
        }

        protected abstract TObject GetItem(int id);
        protected abstract TObject GetItem(string alias, Guid parent);

        // we can't relaibly do this - because names can be the same
        // across the content treee.
        // but we should have overridden all classes that call this 
        // function above.
        protected override TObject GetItem(string alias)
            => default(TObject);

        protected virtual string GetItemPath(TObject item)
        {
            var entity = entityService.Get(item.Id);
            return GetItemPath(entity);
        }
        
        protected virtual string GetItemPath(IEntitySlim item)
        {
            var path = "";
            if (item.ParentId != -1)
            {
                var parent = entityService.Get(item.ParentId);
                if (parent != null)
                    path += "/" + GetItemPath(parent);
            }

            return path += item.Name;
        }

        // we don't do containers in this one.
        // but as we are inheriting the tree base
        // (Maybe there should be another class between tree and
        // things like contenttypes, and templates ?
        protected override EntityContainer GetContainer(Guid key)
            => null;

        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
            => Enumerable.Empty<EntityContainer>();

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => Attempt.Fail<OperationResult<OperationResultType, EntityContainer>>();

    }
}
