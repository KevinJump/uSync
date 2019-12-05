using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    /// <summary>
    ///  a base class for properties that nest other content items inside them
    /// </summary>
    /// <remarks>
    ///  this base class can be used to kickstart a value mapper for anything
    ///  that stores other doctypes inside of its own values (NestedContent, DTGE)
    /// </remarks>
    public abstract class SyncNestedValueMapperBase : SyncValueMapperBase
    {
        protected readonly IContentTypeService contentTypeService;
        protected readonly IDataTypeService dataTypeService;

        public SyncNestedValueMapperBase(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService)
        {
            this.contentTypeService = contentTypeService;
            this.dataTypeService = dataTypeService;
        }


        protected IEnumerable<uSyncDependency> GetPropertyDependencies(JObject value,
            IEnumerable<PropertyType> propertyTypes, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            foreach (var propertyType in propertyTypes)
            {
                var propertyValue = value[propertyType.Alias];
                if (propertyValue == null) continue;

                var dataType = dataTypeService.GetDataType(propertyType.DataTypeKey);
                if (dataType == null) continue;

                var mapper = SyncValueMapperFactory.GetMapper(dataType.EditorAlias);
                if (mapper == null) continue;

                dependencies.AddRange(mapper.GetDependencies(propertyValue, dataType.EditorAlias, flags));
            }

            return dependencies;
        }

        /// <summary>
        ///  get all the dependencies for a series of properties
        /// </summary>
        /// <param name="properties">Key, Value pair, of editorAlias, value</param>
        protected IEnumerable<uSyncDependency> GetPropertyDependencies(
            IDictionary<string, object> properties, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            if (properties.Any())
            {
                foreach (var property in properties)
                {
                    var mapper = SyncValueMapperFactory.GetMapper(property.Key);
                    if (mapper != null)
                    {
                        dependencies.AddRange(mapper.GetDependencies(property.Value, property.Key, flags));
                    }
                }
            }

            return dependencies;

        }


        /// <summary>
        ///  Gets the dependency item for a doctype. 
        /// </summary>
        protected uSyncDependency CreateDocTypeDependency(string alias, DependencyFlags flags)
        {
            var item = contentTypeService.Get(alias);
            if (item != null)
            {
                return new uSyncDependency()
                {
                    Name = item.Name,
                    Udi = item.GetUdi(),
                    Order = DependencyOrders.ContentTypes,
                    Flags = flags & ~DependencyFlags.IncludeAncestors,
                    Level = item.Level
                };
            }

            return null;
        }


    }
}
