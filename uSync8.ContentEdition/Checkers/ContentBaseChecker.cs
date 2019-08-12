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
                    Flags = flags,
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
                    Flags = flags,
                    Level = parent.Level
                }); 

                dependencies.AddRange(GetParentDependencies(parent.Id, order - 1, flags));
            }

            return dependencies;
        }

        protected IEnumerable<uSyncDependency> GetChildDepencies(int id, int order, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            var children = entityService.GetChildren(id);

            foreach(var child in children)
            {
                dependencies.Add(new uSyncDependency()
                {
                    Name = child.Name,
                    Udi = Udi.Create(this.ObjectType.GetUdiType(), child.Key),
                    Order = order,
                    Flags = flags | ~DependencyFlags.IncludeAncestors,
                    Level = child.Level
                });

                if (flags.HasFlagAny(DependencyFlags.IncludeLinked | DependencyFlags.IncludeMedia))
                {
                    var contentChild = GetItemById(child.Id);
                    dependencies.AddRange(GetPropertyDependencies(contentChild, flags));
                }

                dependencies.AddRange(GetChildDepencies(child.Id, order + 1, flags));
            }

            return dependencies;
        }

        protected abstract IContentBase GetItemById(int id);


        /// <summary>
        ///  so do we want this ? go through all the picked values in the content, 
        ///  and include them in the things to export ? 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected IEnumerable<uSyncDependency> GetPropertyDependencies(IContentBase item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            var propertyFlags = flags | ~DependencyFlags.IncludeChildren;

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

                        // media means we include media items (the files are checked)
                        if (flags.HasFlag(DependencyFlags.IncludeMedia))
                        {
                            dependencies.AddRange(linkedDependencies.Where(x => x.Udi.EntityType == UdiEntityType.Media));
                        }


                        // dependencies.AddRange(mapper.GetDependencies(value.EditedValue, editorAlias, propertyFlags));
                    }
                }
            }

            return dependencies;
        }

    }
}
