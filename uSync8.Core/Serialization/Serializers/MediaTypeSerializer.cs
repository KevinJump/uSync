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

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("B3073706-5037-4FBD-A015-DF38D61F2934", "MediaTypeSerializer", uSyncConstants.Serialization.MediaType)]
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>,
        ISyncSerializer<IMediaType>
    {
        private readonly IMediaTypeService mediaTypeService;
       

        public MediaTypeSerializer(
            IEntityService entityService, 
            IDataTypeService dataTypeService,
            IMediaTypeService mediaTypeService) 
            : base(entityService, dataTypeService)
        {
            this.mediaTypeService = mediaTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IMediaType item)
        {
            var node = SerializeBase(item);
            var info = SerializeInfo(item);


            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement("Master", parent.Alias,
                    new XAttribute("Key", parent.Key)));
            }
            else if (item.Level != 1)
            {
                // in a folder
                var folderNode = GetFolderNode(mediaTypeService.GetContainers(item));
                if (folderNode != null)
                    info.Add(folderNode);
            }

            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

            node.Add(SerializeProperties(item));
            node.Add(SerializeStructure(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMediaType), ChangeType.Export);
        }

        protected override SyncAttempt<IMediaType> DeserializeCore(XElement node)
        {
            if (IsValid(node))
                throw new ArgumentException("Invalid XML Format");

            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            CleanTabs(item, node);

            mediaTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        public override SyncAttempt<IMediaType> DesrtializeSecondPass(IMediaType item, XElement node)
        {
            DeserializeCompositions(item, node);
            DeserializeStructure(item, node);
            mediaTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import);
        }

        private void DeserializeCompositions(IMediaType item, XElement node)
        {
            var comps = node.Element("Info").Element("Compositions");
            if (comps == null) return;
            List<IContentTypeComposition> compositions = new List<IContentTypeComposition>();

            foreach (var compositionNode in comps.Elements("Composition"))
            {
                var alias = compositionNode.Value;
                var key = compositionNode.Attribute("Key").ValueOrDefault(Guid.Empty);

                var type = LookupByKeyOrAlias(key, alias);
                if (type != null)
                    compositions.Add(type);
            }

            item.ContentTypeComposition = compositions;
        }


        protected override IMediaType LookupByAlias(string alias)
            => mediaTypeService.Get(alias);

        protected override IMediaType LookupById(int id)
            => mediaTypeService.Get(id);

        protected override IMediaType LookupByKey(Guid key)
            => mediaTypeService.Get(key);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => mediaTypeService.CreateContainer(parentId, name);

        protected override EntityContainer GetContainer(Guid key)
            => mediaTypeService.GetContainer(key);

        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
            => mediaTypeService.GetContainers(folder, level);



        protected override bool PropertyExistsOnComposite(IContentTypeBase item, string alias)
        {
            var allTypes = mediaTypeService.GetAll().ToList();

            var allProperties = allTypes
                .Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id))
                .Select(x => x.PropertyTypes)
                .ToList();

            return allProperties.Any(x => x.Any(y => y.Alias == alias));
        }

        protected override IMediaType CreateItem(string alias, IMediaType parent, ITreeEntity treeItem)
        {
            var item = new MediaType(-1)
            {
                Alias = alias
            };

            if (parent != null)
                item.AddContentType(parent);

            item.SetParent(treeItem);

            return item;
        }
    }
}
