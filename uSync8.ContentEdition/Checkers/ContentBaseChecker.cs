using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;
using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Checkers
{
    public abstract class ContentBaseChecker
    {
        protected readonly IEntityService entityService;
        private UmbracoObjectTypes contentTypeObjectType;
        private SyncValueMapperCollection mappers;

        public UmbracoObjectTypes ObjectType { get; protected set; } = UmbracoObjectTypes.Unknown;

        public ContentBaseChecker(IEntityService entityService, 
            UmbracoObjectTypes contentTypeObjectType,
            SyncValueMapperCollection mappers)
        {
            this.entityService = entityService;
            this.contentTypeObjectType = contentTypeObjectType;

            this.mappers = mappers;
        }

        protected uSyncDependency CalcDocTypeDependency(IContentBase item, DependencyFlags flags)
        {
            if (item.ContentType == null) return null;


            var entity = entityService.GetKey(item.ContentTypeId, contentTypeObjectType);

            if (entity.Success)
            {
                var udi = Udi.Create(contentTypeObjectType.GetUdiType(), entity.Result);

                return new uSyncDependency()
                {
                    Name = item.Name,
                    Udi = udi,
                    Order = DependencyOrders.ContentTypes,
                    Flags = flags & ~DependencyFlags.IncludeAncestors,
                    Level = item.Level
                };
            }

            return null;
        }


        protected IEnumerable<uSyncDependency> GetParentDependencies(int id, int order, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            var parent = entityService.GetParent(id);
            if (parent != null)
            {
                dependencies.Add(new uSyncDependency()
                {
                    Name = parent.Name,
                    Udi = Udi.Create(this.ObjectType.GetUdiType(), parent.Key),
                    Order = order,
                    Flags = flags & ~DependencyFlags.IncludeChildren,
                    Level = parent.Level
                }); 

                dependencies.AddRange(GetParentDependencies(parent.Id, order - 1, flags));
            }

            return dependencies;
        }

        protected IEnumerable<uSyncDependency> GetChildDepencies(int id, int order, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            var children = entityService.GetChildren(id).ToList();

            foreach (var item in children.Select((Child, Index) => new { Child, Index }))
            {
                uSyncDependency.FireUpdate($"Content {item.Child.Name}", item.Index, children.Count);

                dependencies.Add(new uSyncDependency()
                {
                    Name = item.Child.Name,
                    Udi = Udi.Create(this.ObjectType.GetUdiType(), item.Child.Key),
                    Order = order,
                    Flags = flags & ~DependencyFlags.IncludeAncestors,
                    Level = item.Child.Level
                });

                if (flags.HasFlagAny(DependencyFlags.IncludeLinked | DependencyFlags.IncludeMedia))
                {
                    var contentChild = GetItemById(item.Child.Id);
                    dependencies.AddRange(GetPropertyDependencies(contentChild, flags));
                }

                dependencies.AddRange(GetChildDepencies(item.Child.Id, order + 1, flags));
            }

            return dependencies;
        }

        protected abstract IContentBase GetItemById(int id);


        protected readonly string[] settingsTypes = new string[]
        {
            UdiEntityType.Macro, UdiEntityType.DocumentType, UdiEntityType.DataType, UdiEntityType.DictionaryItem
        };

        /// <summary>
        ///  so do we want this ? go through all the picked values in the content, 
        ///  and include them in the things to export ? 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected IEnumerable<uSyncDependency> GetPropertyDependencies(IContentBase item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            var propertyFlags = flags
                & ~DependencyFlags.IncludeChildren;              

            foreach (var property in item.Properties)
            {
                var editorAlias = property.PropertyType.PropertyEditorAlias;
                var mapper = mappers.GetSyncMapper(editorAlias);
                if (mapper != null)
                {
                    foreach(var value in property.Values)
                    {
                        var linkedDependencies = mapper.GetDependencies(value.EditedValue, editorAlias, propertyFlags);

                        // include linked means all content we link to 
                        if (flags.HasFlag(DependencyFlags.IncludeLinked))
                        {
                            dependencies.AddRange(linkedDependencies.Where(x => x.Udi.EntityType == UdiEntityType.Document));
                        }

                        // include any settings things we would be dependent on for this property. 
                        if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                        {
                            dependencies.AddRange(linkedDependencies.Where(x => settingsTypes.InvariantContains(x.Udi.EntityType)));
                        }

                        // media means we include media items (the files are checked)
                        if (flags.HasFlag(DependencyFlags.IncludeMedia))
                        {
                            var media = linkedDependencies.Where(x => x.Udi.EntityType == UdiEntityType.Media).ToList();
                            media.ForEach(x => { x.Flags |= DependencyFlags.IncludeAncestors; });

                            dependencies.AddRange(media);
                        }
                    }
                }
            }

            return dependencies;
        }

    }
}
