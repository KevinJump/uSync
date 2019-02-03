using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{
    public abstract class SyncSerializerBase<TObject> : IDiscoverable
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;

        protected SyncSerializerBase(IEntityService entityService)
        {
            // read the attribute
            var thisType = GetType();
            var meta = thisType.GetCustomAttribute<SyncSerializerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"the uSyncSerializer {thisType} requires a {typeof(SyncSerializerAttribute)}");

            Name = meta.Name;
            Id = meta.Id;
            ItemType = meta.ItemType;

            IsTwoPass = meta.IsTwoPass;

            // base services 
            this.entityService = entityService;

        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public string ItemType { get; set; }

        public Type UmbracoObjectType => typeof(TObject);

        public bool IsTwoPass { get; private set; }


        public SyncAttempt<XElement> Serialize(TObject item)
        {
            return SerializeCore(item);
        }

        public SyncAttempt<TObject> Deserialize(XElement node, bool force, bool OnePass)
        {
            if (IsEmpty(node))
            {
                // empty node do nothing...
                return SyncAttempt<TObject>.Succeed(node.GetKey().ToString(), ChangeType.Removed);
            }

            if (!IsValid(node))
                throw new ArgumentException($"XML Not valid for type {ItemType}");
            

            if (force || !IsCurrent(node))
            {
                var result = DeserializeCore(node);
                if (OnePass && result.Success)
                {
                    DesrtializeSecondPass(result.Item, node);
                }

                return result;
            }

            return SyncAttempt<TObject>.Succeed(node.Name.LocalName, default(TObject), ChangeType.NoChange);
        }

        public virtual SyncAttempt<TObject> DesrtializeSecondPass(TObject item, XElement node)
        {
            return SyncAttempt<TObject>.Succeed(nameof(item), item, typeof(TObject), ChangeType.NoChange);

        }

        protected abstract SyncAttempt<XElement> SerializeCore(TObject item);
        protected abstract SyncAttempt<TObject> DeserializeCore(XElement node);

        /// <summary>
        ///  all xml items now have the same top line, this makes 
        ///  it eaiser for use to do lookups, get things like
        ///  keys and aliases for the basic checkers etc, 
        ///  makes the code simpler.
        /// </summary>
        /// <param name="item">Item we care about</param>
        /// <param name="alias">Alias we want to use</param>
        /// <param name="level">Level</param>
        /// <returns></returns>
        protected virtual XElement InitializeBaseNode(TObject item, string alias, int level = 0)
            => new XElement(ItemType,
                new XAttribute("Key", item.Key),
                new XAttribute("Alias", alias),
                new XAttribute("Level", level));

        /// <summary>
        ///  is this a bit of valid xml 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public virtual bool IsValid(XElement node)
            => node.Name.LocalName == this.ItemType 
                && node.GetKey() != Guid.Empty 
                && node.GetAlias() != string.Empty;

        protected bool IsEmpty(XElement node)
            => node.Name.LocalName == uSyncConstants.Serialization.Empty;

        protected (Guid key, string alias) FindKeyAndAlias(XElement node)
        {
            if (IsValid(node))
                return (
                        key : node.Attribute("Key").ValueOrDefault(Guid.Empty),
                        alias : node.Attribute("Alias").ValueOrDefault(string.Empty)
                       );

            return (key: Guid.Empty, alias: string.Empty);
        }

        protected abstract TObject GetItem(Guid key);
        protected abstract TObject GetItem(string alias);

        public virtual TObject GetItem(XElement node)
        {
            var (key, alias) = FindKeyAndAlias(node);

            if (key != Guid.Empty)
            {
                var item = GetItem(key);
                if (item != null) return item;
            }

            if (!string.IsNullOrWhiteSpace(alias))
                return GetItem(alias);

            return default(TObject);
        }

        public bool IsCurrent(XElement node)
        {
            if (node == null) return false;

            var item = GetItem(node);
            if (item == null) return false;

            var newHash = MakeHash(node);

            var currentNode = Serialize(item);
            if (!currentNode.Success) return false;

            var currentHash = MakeHash(currentNode.Item);
            if (string.IsNullOrEmpty(currentHash)) return false;

            return currentHash == newHash;
        }

        public virtual SyncAttempt<XElement> SerializeEmpty(TObject item, string alias)
        {
            var node = new XElement(uSyncConstants.Serialization.Empty,
                new XAttribute("Key", item.Key),
                new XAttribute("Alias", alias));

            return SyncAttempt<XElement>.Succeed("Empty", node, ChangeType.Removed);
        }


        private string MakeHash(XElement node)
        {
            if (node == null) return string.Empty;
            node = CleanseNode(node);

            using (MemoryStream s = new MemoryStream())
            {
                node.Save(s);
                s.Position = 0;
                using (var md5 = MD5.Create())
                {
                    return BitConverter.ToString(
                        md5.ComputeHash(s)).Replace("-", "").ToLower();
                }
            }
        }

        protected virtual XElement CleanseNode(XElement node) => node;


    }
}
