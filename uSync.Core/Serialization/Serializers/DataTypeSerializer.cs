using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.DataTypes;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("C06E92B7-7440-49B7-B4D2-AF2BF4F3D75D", "DataType Serializer", uSyncConstants.Serialization.DataType)]
    public class DataTypeSerializer : SyncContainerSerializerBase<IDataType>, ISyncSerializer<IDataType>
    {
        private readonly IDataTypeService dataTypeService;
        private readonly DataEditorCollection dataEditors;
        private readonly ConfigurationSerializerCollection configurationSerializers;
        private readonly PropertyEditorCollection propertyEditors;
        private readonly IConfigurationEditorJsonSerializer jsonSerializer;

        public DataTypeSerializer(IEntityService entityService, ILogger<DataTypeSerializer> logger,
            IDataTypeService dataTypeService,
            DataEditorCollection dataEditors,
            ConfigurationSerializerCollection configurationSerializers,
            PropertyEditorCollection propertyEditors,
            IConfigurationEditorJsonSerializer jsonSerializer)
            : base(entityService, logger, UmbracoObjectTypes.DataTypeContainer)
        {
            this.dataTypeService = dataTypeService;
            this.dataEditors = dataEditors;
            this.configurationSerializers = configurationSerializers;
            this.propertyEditors = propertyEditors;
            this.jsonSerializer = jsonSerializer;
        }

        protected override SyncAttempt<IDataType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var info = node.Element("Info");
            var name = info.Element("Name").ValueOrDefault(string.Empty);
            var key = node.GetKey();

            var attempt = FindOrCreate(node);
            if (!attempt.Success)
                throw attempt.Exception;

            var details = new List<uSyncChange>();
            var item = attempt.Result;

            // basic
            if (item.Name != name)
            {
                details.AddUpdate("Name", item.Name, name, "Name");
                item.Name = name;
            }

            if (item.Key != key)
            {
                details.AddUpdate("Key", item.Key, key, "Key");
                item.Key = key;
            }

            var editorAlias = info.Element("EditorAlias").ValueOrDefault(string.Empty);
            if (editorAlias != item.EditorAlias)
            {
                // change the editor type.....
                var newEditor = dataEditors.FirstOrDefault(x => x.Alias.InvariantEquals(editorAlias));
                if (newEditor != null)
                {
                    details.AddUpdate("EditorAlias", item.EditorAlias, editorAlias, "EditorAlias");
                    item.Editor = newEditor;
                }
            }

            // removing sort order - as its not used on datatypes, 
            // and can change based on minor things (so gives false out of sync results)

            // item.SortOrder = info.Element("SortOrder").ValueOrDefault(0);
            var dbType = info.Element("DatabaseType").ValueOrDefault(ValueStorageType.Nvarchar);
            if (item.DatabaseType != dbType)
            {
                details.AddUpdate("DatabaseType", item.DatabaseType, dbType, "DatabaseType");
                item.DatabaseType = dbType;
            }

            // config 
            if (ShouldDeserilizeConfig(name, editorAlias, options))
            {
                details.AddRange(DeserializeConfiguration(item, node));
            }

            details.AddNotNull(SetFolderFromElement(item, info.Element("Folder")));

            return SyncAttempt<IDataType>.Succeed(item.Name, item, ChangeType.Import, details);

        }

        private uSyncChange SetFolderFromElement(IDataType item, XElement folderNode)
        {
            var folder = folderNode.ValueOrDefault(string.Empty);
            if (string.IsNullOrWhiteSpace(folder)) return null;

            var container = FindFolder(folderNode.GetKey(), folder);
            if (container != null && container.Id != item.ParentId)
            {
                var change = uSyncChange.Update("", "Folder", container.Id, item.ParentId);

                item.SetParent(container);

                return change;
            }

            return null;
        }


        private IEnumerable<uSyncChange> DeserializeConfiguration(IDataType item, XElement node)
        {
            var config = node.Element("Config").ValueOrDefault(string.Empty);

            if (!string.IsNullOrWhiteSpace(config))
            {
                var changes = new List<uSyncChange>();

                var serializer = this.configurationSerializers.GetSerializer(item.EditorAlias);
                if (serializer == null)
                {
                    var configObject = JsonConvert.DeserializeObject(config, item.Configuration.GetType());
                    if (!IsJsonEqual(item.Configuration, configObject))
                    {
                        changes.AddUpdateJson("Config", item.Configuration, configObject, "Configuration");
                        item.Configuration = configObject;
                    }
                }
                else
                {
                    logger.LogTrace("Deserializing Config via {0}", serializer.Name);
                    var configObject = serializer.DeserializeConfig(config, item.Configuration.GetType());
                    if (!IsJsonEqual(item.Configuration, configObject))
                    {
                        changes.AddUpdateJson("Config", item.Configuration, configObject, "Configuration");
                        item.Configuration = configObject;
                    }
                }

                return changes;
            }

            return Enumerable.Empty<uSyncChange>();

        }

        /// <summary>
        ///  tells us if the json for an object is equal, helps when the config objects don't have their
        ///  own Equals functions
        /// </summary>
        private bool IsJsonEqual(object currentObject, object newObject)
        {
            var currentString = JsonConvert.SerializeObject(currentObject, Formatting.None);
            var newString = JsonConvert.SerializeObject(newObject, Formatting.None);

            return currentString == newString;
        }


        ///////////////////////

        protected override SyncAttempt<XElement> SerializeCore(IDataType item, SyncSerializerOptions options)
        {
            var node = InitializeBaseNode(item, item.Name, item.Level);

            var info = new XElement("Info",
                new XElement("Name", item.Name),
                new XElement("EditorAlias", item.EditorAlias),
                new XElement("DatabaseType", item.DatabaseType));
            // new XElement("SortOrder", item.SortOrder));

            if (item.Level != 1)
            {
                var folderNode = this.GetFolderNode(item); //TODO - CACHE THIS CALL. 
                if (folderNode != null)
                    info.Add(folderNode);
            }

            node.Add(info);

            var config = SerializeConfiguration(item);
            if (config != null)
                node.Add(config);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IDataType), ChangeType.Export);
        }

        protected override IEnumerable<EntityContainer> GetContainers(IDataType item)
            => dataTypeService.GetContainers(item);

        private XElement SerializeConfiguration(IDataType item)
        {
            if (item.Configuration != null)
            {
                var serializer = this.configurationSerializers.GetSerializer(item.EditorAlias);

                string config;
                if (serializer == null)
                {
                    config = JsonConvert.SerializeObject(item.Configuration, Formatting.Indented);
                }
                else
                {
                    logger.LogDebug("Serializing Config via {0}", serializer.Name);
                    config = serializer.SerializeConfig(item.Configuration);
                }

                return new XElement("Config", new XCData(config));
            }

            return null;
        }


        protected override Attempt<IDataType> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var editorType = FindDataEditor(itemType);
            if (editorType == null)
                return Attempt.Fail<IDataType>(null, new ArgumentException($"(Missing Package?) DataEditor {itemType} is not installed"));

            var item = new DataType(editorType, jsonSerializer, -1);

            item.Name = alias;

            if (parent != null)
                item.SetParent(parent);

            return Attempt.Succeed((IDataType)item);
        }

        private IDataEditor FindDataEditor(string alias)
            => propertyEditors.FirstOrDefault(x => x.Alias == alias);

        protected override string GetItemBaseType(XElement node)
            => node.Element("Info").Element("EditorAlias").ValueOrDefault(string.Empty);

        public override IDataType FindItem(int id)
            => dataTypeService.GetDataType(id);

        public override IDataType FindItem(Guid key)
            => dataTypeService.GetDataType(key);

        public override IDataType FindItem(string alias)
            => dataTypeService.GetDataType(alias);

        protected override EntityContainer FindContainer(Guid key)
            => dataTypeService.GetContainer(key);

        protected override IEnumerable<EntityContainer> FindContainers(string folder, int level)
            => dataTypeService.GetContainers(folder, level);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => dataTypeService.CreateContainer(parentId, Guid.NewGuid(), name);

        public override void SaveItem(IDataType item)
        {
            if (item.IsDirty())
                dataTypeService.Save(item);
        }

        public override void Save(IEnumerable<IDataType> items)
        {
            // if we don't trigger then the cache doesn't get updated :(
            dataTypeService.Save(items.Where(x => x.IsDirty()));
        }

        protected override void SaveContainer(EntityContainer container)
            => dataTypeService.SaveContainer(container);

        public override void DeleteItem(IDataType item)
            => dataTypeService.Delete(item);


        public override string ItemAlias(IDataType item)
            => item.Name;



        /// <summary>
        ///  Checks the config to see if we should be deserializing the config element of a data type.
        /// </summary>
        /// <remarks>
        ///   a key value on the handler will allow users to add editorAliases that they don't want the 
        ///   config importing for. 
        ///   e.g - to not import all the colour picker values.
        ///   <code>
        ///      <Add Key="NoConfigEditors" Value="Umbraco.ColorPicker" />
        ///   </code>
        ///   
        ///   To ignore just specific colour pickers (so still import config for other colour pickers)
        ///   <code>
        ///     <Add Key="NoConfigNames" Value="Approved Colour,My Colour Picker" />
        ///   </code>
        /// </remarks>
        private bool ShouldDeserilizeConfig(string itemName, string editorAlias, SyncSerializerOptions options)
        {
            var noConfigEditors = options.GetSetting(
                uSyncConstants.DefaultSettings.NoConfigEditors, 
                uSyncConstants.DefaultSettings.NoConfigEditors_Default);

            if (!string.IsNullOrWhiteSpace(noConfigEditors) && noConfigEditors.InvariantContains(editorAlias))
                return false;

            var noConfigAliases = options.GetSetting(
                uSyncConstants.DefaultSettings.NoConfigNames,
                uSyncConstants.DefaultSettings.NoConfigNames_Default);

            if (!string.IsNullOrWhiteSpace(noConfigAliases) && noConfigAliases.InvariantContains(itemName))
                return false;

            return true;
        }
    }
}
