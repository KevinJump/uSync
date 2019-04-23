﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.PropertyEditors;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("C06E92B7-7440-49B7-B4D2-AF2BF4F3D75D", "DataType Serializer", uSyncConstants.Serialization.DataType)]
    public class DataTypeSerializer : SyncContainerSerializerBase<IDataType>, ISyncSerializer<IDataType>
    {
        private readonly IDataTypeService dataTypeService;
        private IContentSection contentSection;

        public DataTypeSerializer(IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService, IContentSection contentSection)
            : base(entityService, logger, UmbracoObjectTypes.DataTypeContainer)
        {
            this.dataTypeService = dataTypeService;
            this.contentSection = contentSection;
        }

        protected override SyncAttempt<IDataType> DeserializeCore(XElement node)
        {
            var info = node.Element("Info");
            var name = info.Element("Name").ValueOrDefault(string.Empty);

            var key = node.GetKey();

            var item = FindOrCreate(node);
            if (item == null) throw new ArgumentException($"Cannot find underling datatype for {name}");

            // basic
            item.Name = name;
            item.Key = key;

            var editorAlias = info.Element("EditorAlias").ValueOrDefault(string.Empty);
            if (editorAlias != item.EditorAlias)
            {
                // change the editor type.....
                var newEditor = Current.DataEditors.FirstOrDefault(x => x.Name.InvariantEquals(editorAlias));
                if (newEditor != null)
                {
                    item.Editor = newEditor;
                }
            }

            item.SortOrder = info.Element("SortOrder").ValueOrDefault(0);
            item.DatabaseType = info.Element("DatabaseType").ValueOrDefault(ValueStorageType.Nvarchar);

            // config 
            DeserializeConfiguration(item, node);

            SetFolderFromElement(item, info.Element("Folder"));

            dataTypeService.Save(item);

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
                // should be one of thesE ? 
                // item.Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(config);
                item.Configuration = JsonConvert.DeserializeObject(config, item.Configuration.GetType());
            }
        }
  

        ///////////////////////

        protected override SyncAttempt<XElement> SerializeCore(IDataType item)
        {
            var node = InitializeBaseNode(item, item.Name, item.Level);

            var info = new XElement("Info",
                new XElement("Name", item.Name),
                new XElement("EditorAlias", item.EditorAlias),
                new XElement("DatabaseType", item.DatabaseType),
                new XElement("SortOrder", item.SortOrder));

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
            // item.Configuration;
            // item.Configuration
            if (item.Configuration != null)
            {
                return new XElement("Config", new XCData(JsonConvert.SerializeObject(item.Configuration, Formatting.Indented)));
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
        {
            return Current.PropertyEditors
                .FirstOrDefault(x => x.Alias == alias);
                
        }

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

        protected override void SaveContainer(EntityContainer container)
            => dataTypeService.SaveContainer(container);
    }
}
