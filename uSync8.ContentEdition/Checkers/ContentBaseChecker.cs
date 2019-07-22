using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Checkers
{
    public class ContentBaseChecker
    {
        protected readonly IEntityService entityService;
        protected int baseOrder;

        private UmbracoObjectTypes contentTypeObjectType;

        public UmbracoObjectTypes ObjectType { get; protected set; } = UmbracoObjectTypes.Unknown;

        public ContentBaseChecker(IEntityService entityService, int baseOrder, UmbracoObjectTypes contentTypeObjectType)
        {
            this.entityService = entityService;
            this.baseOrder = baseOrder;

            this.contentTypeObjectType = contentTypeObjectType;
        }

        protected uSyncDependency CalcDocTypeDependency(IContentBase item)
        {
            if (item.ContentType == null) return null;


            var entity = entityService.GetKey(item.ContentTypeId, contentTypeObjectType);

            if (entity.Success)
            {
                var udi = Udi.Create(contentTypeObjectType.GetUdiType(), entity.Result);

                return new uSyncDependency()
                {
                    Udi = udi,
                    Order = DependencyOrders.ContentTypes
                };
            }

            return null;
        }


        protected IEnumerable<uSyncDependency> GetParentDependencies(int id)
        {
            var dependencies = new List<uSyncDependency>();

            var parent = entityService.GetParent(id);
            if (parent != null)
            {
                dependencies.Add(new uSyncDependency()
                {
                    Udi = Udi.Create(this.ObjectType.GetUdiType(), parent.Key),
                    Order = baseOrder
                }); 

                dependencies.AddRange(GetParentDependencies(parent.Id));
            }

            return dependencies;
        }

        protected IEnumerable<uSyncDependency> GetChildDepencies(int id)
        {
            var dependencies = new List<uSyncDependency>();

            var children = entityService.GetChildren(id);

            foreach(var child in children)
            {
                dependencies.Add(new uSyncDependency()
                {
                    Udi = Udi.Create(this.ObjectType.GetUdiType(), child.Key),
                    Order = baseOrder
                });

                dependencies.AddRange(GetChildDepencies(child.Id));
            }

            return dependencies;
        }


        /// <summary>
        ///  so do we want this ? go through all the picked values in the content, 
        ///  and include them in the things to export ? 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected IEnumerable<uSyncDependency> GetPropertyDependencies(IContentBase item)
        {
            var dependencies = new List<uSyncDependency>();

            foreach (var property in item.Properties)
            {

            }

            return dependencies;

        }

    }
}
