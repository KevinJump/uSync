using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using uSync.Core.Models;

namespace uSync.Core.Serialization
{
    public abstract class SyncSerializerRoot<TObject>
    {
        protected readonly ILogger<SyncSerializerRoot<TObject>> logger;

        protected readonly Type serializerType;

        protected SyncSerializerRoot(ILogger<SyncSerializerRoot<TObject>> logger)
        {
            this.logger = logger;

            // read the attribute
            serializerType = this.GetType();
            var meta = serializerType.GetCustomAttribute<SyncSerializerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"the uSyncSerializer {serializerType} requires a {typeof(SyncSerializerAttribute)}");

            Name = meta.Name;
            Id = meta.Id;
            ItemType = meta.ItemType;

            IsTwoPass = meta.IsTwoPass;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public string ItemType { get; set; }

        public Type objectType => typeof(TObject);

        public bool IsTwoPass { get; private set; }

        public SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options)
        {
            return this.SerializeCore(item, options);
        }

        /// <summary>
        ///  CanDeserialize based on the flags, used to check the model is good, for this import 
        /// </summary>
        protected virtual SyncAttempt<TObject> CanDeserialize(XElement node, SyncSerializerOptions options)
            => SyncAttempt<TObject>.Succeed("No Check", ChangeType.NoChange);


        public SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options)
        {
            if (node.IsEmptyItem())
            {
                // new behavior when a node is 'empty' that is a marker for a delete or rename
                // so we process that action here, no more action file/folders
                return ProcessAction(node, options);
            }

            if (!IsValid(node))
                throw new FormatException($"XML Not valid for type {ItemType}");

            if (options.Force || IsCurrent(node, options) > ChangeType.NoChange)
            {
                // pre-deserilzation check. 
                var check = CanDeserialize(node, options);
                if (!check.Success) return check;

                logger.LogDebug("Base: Deserializing {0}", ItemType);
                var result = DeserializeCore(node, options);

                if (result.Success)
                {
                    logger.LogDebug("Base: Deserialize Core Success {0}", ItemType);

                    if (!options.Flags.HasFlag(SerializerFlags.DoNotSave))
                    {
                        logger.LogDebug("Base: Serializer Saving (No DoNotSaveFlag) {0}", ItemAlias(result.Item));
                        // save 
                        SaveItem(result.Item);
                    }

                    if (options.OnePass)
                    {
                        logger.LogDebug("Base: Processing item in one pass {0}", ItemAlias(result.Item));

                        var secondAttempt = DeserializeSecondPass(result.Item, node, options);

                        logger.LogDebug("Base: Second Pass Result {0} {1}", ItemAlias(result.Item), secondAttempt.Success);

                        // if its the second pass, we return the results of that pass
                        return secondAttempt;
                    }
                }

                return result;
            }

            return SyncAttempt<TObject>.Succeed(node.GetAlias(), ChangeType.NoChange);
        }

        public virtual SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options)
        {
            return SyncAttempt<TObject>.Succeed(nameof(item), item, typeof(TObject), ChangeType.NoChange);
        }

        protected abstract SyncAttempt<XElement> SerializeCore(TObject item, SyncSerializerOptions options);
        protected abstract SyncAttempt<TObject> DeserializeCore(XElement node, SyncSerializerOptions options);

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
                new XAttribute("Key", ItemKey(item).ToString().ToLower()),
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

 
        /// <summary>
        ///  Is the XML either valid or 'Empty' 
        /// </summary>
        /// <param name="node">XML to examine</param>
        /// <returns>true if file is valid or empty</returns>
        public bool IsValidOrEmpty(XElement node)
            => node.IsEmptyItem() || IsValid(node);

        /// <summary>
        ///  Process the action in teh 'empty' XML node
        /// </summary>
        /// <param name="node">XML to process</param>
        /// <param name="flags">Serializer flags to control options</param>
        /// <returns>Sync attempt detailing changes</returns>
        protected SyncAttempt<TObject> ProcessAction(XElement node, SyncSerializerOptions options)

        {
            if (!node.IsEmptyItem())
                throw new ArgumentException("Cannot process actions on a non-empty node");

            var actionType = node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);

            logger.LogDebug("Empty Node : Processing Action {0}", actionType);

            var (key, alias) = FindKeyAndAlias(node);

            switch (actionType)
            {
                case SyncActionType.Delete:
                    if (options.DeleteItems())
                        return ProcessDelete(key, alias, options.Flags);

                    return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
                case SyncActionType.Rename:
                    return ProcessRename(key, alias, options.Flags);
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
            logger.LogDebug("Processing Delete {0} {1}", key, alias);

            var item = this.FindItem(key);
            if (item == null && !string.IsNullOrWhiteSpace(alias))
            {
                // we need to build in some awareness of alias matching in the folder
                // because if someone deletes something in one place and creates it 
                // somewhere else the alias will exist, so we don't want to delete 
                // it from over there - this needs to be done at save time 
                // (bascially if a create happens) - turn any delete files into renames

                // A Tree Based serializer will return null if you ask it to find 
                // an item soley by alias, so this means we are only deleting by key 
                // on tree's (e.g media, content)
                item = this.FindItem(alias);
            }

            if (item != null)
            {
                logger.LogDebug("Deleting Item : {0}", ItemAlias(item));
                DeleteItem(item);
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.Delete);
            }

            logger.LogDebug("Delete Item not found");
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        protected virtual SyncAttempt<TObject> ProcessRename(Guid key, string alias, SerializerFlags flags)
        {
            logger.LogDebug("Process Rename (no action)");
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        public virtual ChangeType IsCurrent(XElement node, SyncSerializerOptions options)
        {
            XElement current = null;
            var item = FindItem(node);
            if (item != null)
            {
                var attempt = this.Serialize(item, options);
                if (attempt.Success)
                    current = attempt.Item;
            }

            return IsCurrent(node, current, options);
        }

        public virtual ChangeType IsCurrent(XElement node, XElement current, SyncSerializerOptions options)
        {
            if (node == null) return ChangeType.Update;

            if (!IsValidOrEmpty(node)) throw new FormatException($"Invalid Xml File {node.Name.LocalName}");

            if (current == null)
            {
                if (node.IsEmptyItem())
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

            if (node.IsEmptyItem()) return CalculateEmptyChange(node, current);

            var newHash = MakeHash(node);

            var currentHash = MakeHash(current);
            if (string.IsNullOrEmpty(currentHash)) return ChangeType.Update;

            return currentHash == newHash ? ChangeType.NoChange : ChangeType.Update;
        }

        private ChangeType CalculateEmptyChange(XElement node, XElement current)
        {
            // this shouldn't happen, but check.
            if (current == null) return ChangeType.NoChange;

            // simple logic, if it's a delete we say so, 
            // renames are picked up by the check on the new file

            switch (node.GetEmptyAction())
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
            logger.LogDebug("Base: Serializing Empty Element {alias} {change}", alias, change);

            if (string.IsNullOrEmpty(alias))
                alias = ItemAlias(item);

            var node = XElementExtensions.MakeEmpty(ItemKey(item), change, alias);

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

        public abstract TObject FindItem(int id);

        public abstract TObject FindItem(Guid key);
        public abstract TObject FindItem(string alias);

        public abstract void SaveItem(TObject item);

        public abstract void DeleteItem(TObject item);

        public abstract string ItemAlias(TObject item);

        public abstract Guid ItemKey(TObject item);


        /// <summary>
        ///  for bulk saving, some services do this, it causes less cache hits and 
        ///  so should be faster. 
        /// </summary>
        public virtual void Save(IEnumerable<TObject> items)
        {
            foreach (var item in items)
            {
                this.SaveItem(item);
            }
        }

        public virtual TObject FindItem(XElement node)
        {
            var (key, alias) = FindKeyAndAlias(node);

            logger.LogTrace("Base: Find Item {0} [{1}]", key, alias);

            if (key != Guid.Empty)
            {
                var item = FindItem(key);
                if (item != null) return item;
            }

            if (!string.IsNullOrWhiteSpace(alias))
            {
                logger.LogTrace("Base: Lookup by Alias: {0}", alias);
                return FindItem(alias);
            }

            return default(TObject);
        }
        #endregion

    }
}
