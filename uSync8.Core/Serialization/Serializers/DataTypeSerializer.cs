using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("C06E92B7-7440-49B7-B4D2-AF2BF4F3D75D", "DataType Serializer", uSyncConstants.Serialization.DataType)]
    public class DataTypeSerializer : SyncTreeSerializerBase<IDataType>, ISyncSerializer<IDataType>
    {
        private readonly IDataTypeService dataTypeService;

        public DataTypeSerializer(IEntityService entityService,
            IDataTypeService dataTypeService)
            : base(entityService)
        {
            this.dataTypeService = dataTypeService;
        }

        protected override SyncAttempt<IDataType> DeserializeCore(XElement node)
        {
            var info = node.Element("Info");
            var name = info.Element("Name").ValueOrDefault(string.Empty);

            var key = node.GetKey();

            var item = FindOrCreate(node);

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
            // item.DatabaseType = info.Element("DatabaseType").ValueOrDefault(ValueStorageType.Nvarchar);

            // config 
            DeserializeConfiguration(item, node);

            dataTypeService.Save(item);

            return SyncAttempt<IDataType>.Succeed(item.Name, item, ChangeType.Import);

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
            

        protected override IDataType CreateItem(string alias, IDataType parent, ITreeEntity treeItem, string itemType)
        {
            var editorType = Current.DataEditors.FirstOrDefault(x => x.Name.InvariantEquals(itemType));
            if (editorType == null) return null;

            var item = new DataType(editorType, -1)
            {
                Name = alias
            };

            if (treeItem != null)
                item.SetParent(parent);

            return item;
        }

        protected override string GetItemBaseType(XElement node)
            => node.Element("Info").Element("EditorAlias").ValueOrDefault(string.Empty);

        protected override IDataType GetItem(Guid key)
            => dataTypeService.GetDataType(key);

        protected override IDataType GetItem(string alias)
            => dataTypeService.GetDataType(alias);

        protected override EntityContainer GetContainer(Guid key)
            => dataTypeService.GetContainer(key);

        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
            => dataTypeService.GetContainers(folder, level);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => dataTypeService.CreateContainer(parentId, name);
    }
}
