using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;

using Umbraco.Core.Logging;

using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{
    public abstract class SyncSerializerRoot<TObject>
    {
        protected readonly ILogger logger;

        protected readonly Type serializerType;

        protected SyncSerializerRoot(ILogger logger)
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


        [Obsolete("Serialize with options for better config support")]
        public SyncAttempt<XElement> Serialize(TObject item)
            => Serialize(item, new SyncSerializerOptions());

        public SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options)
        {
            return this.SerializeCore(item, options);
        }

        /// <remarks>
        ///  used primarliy for checking parentage, but also can be used for checking things like create only.
        /// </remarks>
        [Obsolete("deserialize with options for better config support")]
        protected virtual SyncAttempt<TObject> CanDeserialize(XElement node, SerializerFlags flags)
            => CanDeserialize(node, new SyncSerializerOptions(flags));

        /// <summary>
        ///  CanDeserialize based on the flags, used to check the model is good, for this import 
        /// </summary>
        protected virtual SyncAttempt<TObject> CanDeserialize(XElement node, SyncSerializerOptions options)
            => SyncAttempt<TObject>.Succeed("No Check", ChangeType.NoChange);


        [Obsolete("deserialize with options for better config support")]
        public SyncAttempt<TObject> Deserialize(XElement node, SerializerFlags flags)
            => Deserialize(node, new SyncSerializerOptions(flags));

        public SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options)
        {
            if (IsEmpty(node))
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

                logger.Debug(serializerType, "Base: Deserializing {0}", ItemType);
                var result = DeserializeCore(node, options);

                if (result.Success)
                {
                    logger.Debug(serializerType, "Base: Deserialize Core Success {0}", ItemType);

                    if (!options.Flags.HasFlag(SerializerFlags.DoNotSave))
                    {
                        logger.Debug(serializerType, "Base: Serializer Saving (No DoNotSaveFlag) {0}", ItemAlias(result.Item));
                        // save 
                        SaveItem(result.Item);
                    }

                    if (options.OnePass)
                    {
                        logger.Debug(serializerType, "Base: Processing item in one pass {0}", ItemAlias(result.Item));

                        var secondAttempt = DeserializeSecondPass(result.Item, node, options);

                        logger.Debug(serializerType, "Base: Second Pass Result {0} {1}", ItemAlias(result.Item), secondAttempt.Success);

                        // if its the second pass, we return the results of that pass
                        return secondAttempt;
                    }
                }

                return result;
            }

            return SyncAttempt<TObject>.Succeed(node.GetAlias(), ChangeType.NoChange);
        }

        [Obsolete("Deserialize with options for better config support")]
        public virtual SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SerializerFlags flags)
            => DeserializeSecondPass(item, node, new SyncSerializerOptions(flags));

        public virtual SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options)
        {
            return SyncAttempt<TObject>.Succeed(nameof(item), item, typeof(TObject), ChangeType.NoChange);
        }


        [Obsolete("Pass SyncSerializerOptions for more config control")]
        protected virtual SyncAttempt<XElement> SerializeCore(TObject item)
            => SerializeCore(item, new SyncSerializerOptions());

        [Obsolete("Pass SyncSerializerOptions for more config control")]
        protected virtual SyncAttempt<TObject> DeserializeCore(XElement node)
            => DeserializeCore(node, new SyncSerializerOptions());

#pragma warning disable CS0618 // Type or member is obsolete

        protected virtual SyncAttempt<XElement> SerializeCore(TObject item, SyncSerializerOptions options)
            => SerializeCore(item);

        protected virtual SyncAttempt<TObject> DeserializeCore(XElement node, SyncSerializerOptions options)
            => DeserializeCore(node);

#pragma warning restore CS0618 // Type or member is obsolete

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
        ///  Does the XML represent an Empty uSync Action?
        /// </summary>
        /// <param name="node">XML to examine</param>
        /// <returns>true if the file is an 'empty' file</returns>
        public bool IsEmpty(XElement node)
            => node.Name.LocalName == uSyncConstants.Serialization.Empty;

        /// <summary>
        ///  Is the XML either valid or 'Empty' 
        /// </summary>
        /// <param name="node">XML to examine</param>
        /// <returns>true if file is valid or empty</returns>
        public bool IsValidOrEmpty(XElement node)
            => IsEmpty(node) || IsValid(node);

        [Obsolete]
        protected SyncAttempt<TObject> ProcessAction(XElement node, SerializerFlags flags)
            => ProcessAction(node, new SyncSerializerOptions(flags));

        /// <summary>
        ///  Process the action in teh 'empty' XML node
        /// </summary>
        /// <param name="node">XML to process</param>
        /// <param name="flags">Serializer flags to control options</param>
        /// <returns>Sync attempt detailing changes</returns>
        protected SyncAttempt<TObject> ProcessAction(XElement node, SyncSerializerOptions options)

        {
            if (!IsEmpty(node))
                throw new ArgumentException("Cannot process actions on a non-empty node");

            var actionType = node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);

            logger.Debug(serializerType, "Empty Node : Processing Action {0}", actionType);

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
            logger.Debug(serializerType, "Processing Delete {0} {1}", key, alias);

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
                logger.Debug(serializerType, "Deleting Item : {0}", ItemAlias(item));
                DeleteItem(item);
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.Delete);
            }

            logger.Debug(serializerType, "Delete Item not found");
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        protected virtual SyncAttempt<TObject> ProcessRename(Guid key, string alias, SerializerFlags flags)
        {
            logger.Debug(serializerType, "Process Rename (no action)");
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }

        [Obsolete]
        public virtual ChangeType IsCurrent(XElement node)
            => IsCurrent(node, new SyncSerializerOptions());

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

            if (IsEmpty(node)) return CalculateEmptyChange(node, current);

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
            logger.Debug(serializerType, "Base: Serializing Empty Element {alias} {change}", alias, change);

            if (string.IsNullOrEmpty(alias))
                alias = ItemAlias(item);

            var node = XElementExtensions.MakeEmpty(ItemKey(item), change, alias);
            node.Add(new XAttribute("Level", change == SyncActionType.Delete ? 9999 : 9998));

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
                using (var hashAlgorithm = uSyncHashAlgorithm.Create())
                {
                    return BitConverter.ToString(
                        hashAlgorithm.ComputeHash(s)).Replace("-", "").ToLower();
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

        protected abstract Guid ItemKey(TObject item);


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

            logger.Verbose(serializerType, "Base: Find Item {0} [{1}]", key, alias);

            if (key != Guid.Empty)
            {
                var item = FindItem(key);
                if (item != null) return item;
            }

            if (!string.IsNullOrWhiteSpace(alias))
            {
                logger.Verbose(serializerType, "Base: Lookup by Alias: {0}", alias);
                return FindItem(alias);
            }

            return default(TObject);
        }
        #endregion

    }
}
