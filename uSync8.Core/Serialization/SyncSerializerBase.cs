using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
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
        protected readonly ILogger logger;

        protected SyncSerializerBase(
            IEntityService entityService, ILogger logger)
        {
            this.logger = logger;

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

        public Type objectType => typeof(TObject);

        public bool IsTwoPass { get; private set; }


        public SyncAttempt<XElement> Serialize(TObject item)
        {
            return SerializeCore(item);
        }
     

        public SyncAttempt<TObject> Deserialize(XElement node, SerializerFlags flags)
        {
            if (IsEmpty(node))
            {
                // new behavior when a node is 'empty' that is a marker for a delete or rename
                // so we process that action here, no more action file/folders
                return ProcessAction(node, flags);
            }

            if (!IsValid(node))
                throw new FormatException($"XML Not valid for type {ItemType}");


            if ( flags.HasFlag(SerializerFlags.Force) || IsCurrent(node) > ChangeType.NoChange)
            {
                logger.Debug<TObject>("Base: Deserializing");
                var result = DeserializeCore(node);

                if (result.Success)
                {
                    if (!flags.HasFlag(SerializerFlags.DoNotSave))
                    {
                        // save 
                        SaveItem(result.Item);
                    }

                    if (flags.HasFlag(SerializerFlags.OnePass))
                    {
                        logger.Debug<TObject>("Base: Second Pass");
                        var secondAttempt = DeserializeSecondPass(result.Item, node, flags);

                        // if its the second pass, we return the results of that pass
                        return secondAttempt;
                    }
                }

                return result;
            }

            return SyncAttempt<TObject>.Succeed(node.GetAlias(), default(TObject), ChangeType.NoChange);
        }

        public virtual SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SerializerFlags flags)
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
                new XAttribute("Key", item.Key.ToString().ToLower()),
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

        public bool IsEmpty(XElement node)
            => node.Name.LocalName == uSyncConstants.Serialization.Empty;

        public bool IsValidOrEmpty(XElement node)
            => IsEmpty(node) || IsValid(node);

        protected SyncAttempt<TObject> ProcessAction(XElement node, SerializerFlags flags)
        {
            if (!IsEmpty(node))
                throw new ArgumentException("Cannot process actions on a non-empty node");

            var actionType = node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);

            var (key, alias) = FindKeyAndAlias(node);
            
            switch(actionType)
            {
                case SyncActionType.Delete:
                    return ProcessDelete(key, alias, flags);
                case SyncActionType.Rename:
                    return ProcessRename(key, alias, flags);
                case SyncActionType.Clean:
                    // we return a 'clean' success, but this is then picked up 
                    // in the handler, as something to clean, so the handler does it. 
                    return SyncAttempt<TObject>.Succeed(alias, ChangeType.Clean);
                default:
                    return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
            }
        }

        protected virtual SyncAttempt<TObject> ProcessDelete(Guid key, string alias, SerializerFlags flags)
        {
            var item = this.FindItem(key);
            if (item == null && !string.IsNullOrWhiteSpace(alias))
            {
                // we need to build in some awareness of alias matching in the folder
                // because if someone deletes something in one place and creates it 
                // somewhere else the alias will exist, so we don't want to delete 
                // it from over there - this needs to be done at save time 
                // (bascially if a create happens) - turn any delete files into renames
                item = this.FindItem(alias);
            }

            if (item != null)
            {
                DeleteItem(item);
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.Delete);
            }

            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        protected virtual SyncAttempt<TObject> ProcessRename(Guid key, string alias, SerializerFlags flags)
        {
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        public ChangeType IsCurrent(XElement node)
        {
            if (node == null) return ChangeType.Update;

            if (!IsValidOrEmpty(node)) throw new FormatException($"Invalid Xml File {node.Name.LocalName}");

            var item = FindItem(node);
            if (item == null)
            {
                if (IsEmpty(node))
                {
                    // we tell people it's a clean.
                    if (node.GetEmptyAction() == SyncActionType.Clean) return ChangeType.Clean;

                    // at this point its possible the file is for a rename or delete that has already happened
                    return ChangeType.NoChange;
                }
                else
                {
                    return ChangeType.Create;
                }
            }

            if (IsEmpty(node)) return CalculateEmptyChange(node, item);

            var newHash = MakeHash(node);

            var currentNode = Serialize(item);
            if (!currentNode.Success) return ChangeType.Create;

            var currentHash = MakeHash(currentNode.Item);
            if (string.IsNullOrEmpty(currentHash)) return ChangeType.Update;

            return currentHash == newHash ? ChangeType.NoChange : ChangeType.Update;
        }

        private ChangeType CalculateEmptyChange(XElement node, TObject item)
        {
            // this shouldn't happen, but check.
            if (item == null) return ChangeType.NoChange;

            // simple logic, if it's a delete we say so, 
            // renames are picked up by the check on the new file

            switch(node.GetEmptyAction())
            {
                case SyncActionType.Delete:
                    return ChangeType.Delete;
                case SyncActionType.Clean:
                    return ChangeType.Clean;
                default:
                    return ChangeType.NoChange;
            }
        }

        public virtual SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias)
        {
            logger.Debug<TObject>("Base: Serializing Empty Element (Delete or rename) {0}", alias);

            if (string.IsNullOrEmpty(alias))
                alias = ItemAlias(item);

            var node = XElementExtensions.MakeEmpty(item.Key, change, alias);

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

        /// <summary>
        ///  cleans up the node, removing things that are not generic (like internal Ids)
        ///  so that the comparisions are like for like.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual XElement CleanseNode(XElement node) => node;


        #region Finders 
        // Finders - used on importing, getting things that are already there (or maybe not)

        protected (Guid key, string alias) FindKeyAndAlias(XElement node)
        {
            if (IsValidOrEmpty(node))
                return (
                        key: node.Attribute("Key").ValueOrDefault(Guid.Empty),
                        alias: node.Attribute("Alias").ValueOrDefault(string.Empty)
                       );

            return (key: Guid.Empty, alias: string.Empty);
        }

        protected abstract TObject FindItem(Guid key);
        protected abstract TObject FindItem(string alias);

        protected abstract void SaveItem(TObject item);

        protected abstract void DeleteItem(TObject item);

        protected abstract string ItemAlias(TObject item);

        /// <summary>
        ///  for bulk saving, some services do this, it causes less cache hits and 
        ///  so should be faster. 
        /// </summary>
        public virtual void Save(IEnumerable<TObject> items)
        {
            foreach(var item in items)
            {
                this.SaveItem(item);
            }
        }

        public virtual TObject FindItem(XElement node)
        {
            var (key, alias) = FindKeyAndAlias(node);

            logger.Debug<TObject>("Base: Find Item {0} [{1}]", key, alias);

            if (key != Guid.Empty)
            {
                var item = FindItem(key);
                if (item != null) return item;
            }

            if (!string.IsNullOrWhiteSpace(alias))
            {
                logger.Debug<TObject>("Base: Lookup by Alias: {0}", alias);
                return FindItem(alias);
            }

            return default(TObject);
        }
        #endregion

    }
}
