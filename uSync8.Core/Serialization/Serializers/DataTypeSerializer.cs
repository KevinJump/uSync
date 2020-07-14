using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;

using uSync8.Core.DataTypes;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("C06E92B7-7440-49B7-B4D2-AF2BF4F3D75D", "DataType Serializer", uSyncConstants.Serialization.DataType)]
    public class DataTypeSerializer : SyncContainerSerializerBase<IDataType>, ISyncOptionsSerializer<IDataType>
    {
        private readonly IDataTypeService dataTypeService;
        private readonly ConfigurationSerializerCollection configurationSerializers;

        public DataTypeSerializer(IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            ConfigurationSerializerCollection configurationSerializers)
            : base(entityService, logger, UmbracoObjectTypes.DataTypeContainer)
        {
            this.dataTypeService = dataTypeService;
            this.configurationSerializers = configurationSerializers;
        }

        protected override SyncAttempt<IDataType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var info = node.Element("Info");
            var name = info.Element("Name").ValueOrDefault(string.Empty);

            var key = node.GetKey();

            var item = FindOrCreate(node);
            if (item == null) throw new ArgumentException($"Cannot find underling datatype for {name}");

            // basic
            if (item.Name != name)
                item.Name = name;

            if (item.Key != key)
                item.Key = key;

            var editorAlias = info.Element("EditorAlias").ValueOrDefault(string.Empty);
            if (editorAlias != item.EditorAlias)
            {
                // change the editor type.....
                var newEditor = Current.DataEditors.FirstOrDefault(x => x.Alias.InvariantEquals(editorAlias));
                if (newEditor != null)
                {
                    item.Editor = newEditor;
                }
            }

            // removing sort order - as its not used on datatypes, 
            // and can change based on minor things (so gives false out of sync results)

            // item.SortOrder = info.Element("SortOrder").ValueOrDefault(0);
            var dbType = info.Element("DatabaseType").ValueOrDefault(ValueStorageType.Nvarchar);
            if (item.DatabaseType != dbType)
                item.DatabaseType = dbType;

            // config 
            DeserializeConfiguration(item, node);

            SetFolderFromElement(item, info.Element("Folder"));

            // save is responsiblity of caller 
            // dataTypeService.Save(item);

            return SyncAttempt<IDataType>.Succeed(item.Name, item, ChangeType.Import);

        }

        private void SetFolderFromElement(IDataType item, XElement folderNode)
        {
            var folder = folderNode.ValueOrDefault(string.Empty);
            if (string.IsNullOrWhiteSpace(folder)) return;

            var container = FindFolder(folderNode.GetKey(), folder);
            if (container != null && container.Id != item.ParentId)
            {
                item.SetParent(container);
            }
        }


        private void DeserializeConfiguration(IDataType item, XElement node)
        {
            var config = node.Element("Config").ValueOrDefault(string.Empty);

            if (!string.IsNullOrWhiteSpace(config))
            {
                var serializer = this.configurationSerializers.GetSerializer(item.EditorAlias);
                if (serializer == null)
                {
                    var configObject = JsonConvert.DeserializeObject(config, item.Configuration.GetType());
                    if (!IsJsonEqual(item.Configuration, configObject))
                        item.Configuration = configObject;
                }
                else
                {
                    logger.Verbose<DataTypeSerializer>("Deserializing Config via {0}", serializer.Name);
                    var configObject = serializer.DeserializeConfig(config, item.Configuration.GetType());
                    if (!IsJsonEqual(item.Configuration, configObject))
                        item.Configuration = configObject;
                }
            }
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
                var folderNode = this.GetFolderNode(dataTypeService.GetContainers(item));
                if (folderNode != null)
                    info.Add(folderNode);
            }

            node.Add(info);

            var config = SerializeConfiguration(item);
            if (config != null)
                node.Add(config);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IDataType), ChangeType.Export);
        }

        private XElement SerializeConfiguration(IDataType item)
        {
            if (item.Configuration != null)
            {
                var serializer = this.configurationSerializers.GetSerializer(item.EditorAlias);

                string config ;
                if (serializer == null)
                {
                    config = JsonConvert.SerializeObject(item.Configuration, Formatting.Indented);
                }
                else
                {
                    logger.Verbose<DataTypeSerializer>("Serializing Config via {0}", serializer.Name);
                    config = serializer.SerializeConfig(item.Configuration);
                }

                return new XElement("Config", new XCData(config));
            }

            return null;
        }


        protected override IDataType CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var editorType = FindDataEditor(itemType);
            if (editorType == null) return null;

            var item = new DataType(editorType, -1)
            {
                Name = alias
            };

            if (parent != null)
                item.SetParent(parent);

            return item;
        }

        private IDataEditor FindDataEditor(string alias)
            => Current.PropertyEditors.FirstOrDefault(x => x.Alias == alias);

        protected override string GetItemBaseType(XElement node)
            => node.Element("Info").Element("EditorAlias").ValueOrDefault(string.Empty);

        protected override IDataType FindItem(Guid key)
            => dataTypeService.GetDataType(key);

        protected override IDataType FindItem(string alias)
            => dataTypeService.GetDataType(alias);

        protected override EntityContainer FindContainer(Guid key)
            => dataTypeService.GetContainer(key);

        protected override IEnumerable<EntityContainer> FindContainers(string folder, int level)
            => dataTypeService.GetContainers(folder, level);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> FindContainers(int parentId, string name)
            => dataTypeService.CreateContainer(parentId, name);

        protected override void SaveItem(IDataType item)
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

        protected override void DeleteItem(IDataType item)
            => dataTypeService.Delete(item);


        protected override string ItemAlias(IDataType item)
            => item.Name;
    }
}
