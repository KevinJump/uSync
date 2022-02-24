﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
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

        protected readonly Lazy<SyncValueMapperCollection> mapperCollection;

        public SyncNestedValueMapperBase(IEntityService entityService,
            Lazy<SyncValueMapperCollection> mapperCollection,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService)
        {
            this.mapperCollection = mapperCollection;

            this.contentTypeService = contentTypeService;
            this.dataTypeService = dataTypeService;
        }

        /// <summary>
        ///  get the export value for the properties used in this JObject
        /// </summary>
        protected JObject GetExportProperties(JObject item, IContentType docType)
        {
            foreach (var property in docType.CompositionPropertyTypes)
            {
                if (item.ContainsKey(property.Alias))
                {
                    var value = item[property.Alias];
                    if (value != null)
                    {
                        var mappedVal = mapperCollection.Value.GetExportValue(value, property.PropertyEditorAlias);
                        item[property.Alias] = mappedVal; // .GetJsonTokenValue();
                    }
                }
            }

            return item;
        }

        protected IEnumerable<uSyncDependency> GetPropertyDependencies(JObject value,
            IContentType docType, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            foreach (var propertyType in docType.CompositionPropertyTypes)
            {
                var propertyValue = value[propertyType.Alias];
                if (propertyValue == null) continue;

                var dataType = dataTypeService.GetDataType(propertyType.DataTypeKey);
                if (dataType == null) continue;

                dependencies.AddRange(mapperCollection.Value.GetDependencies(propertyValue, dataType.EditorAlias, flags));
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
                    dependencies.AddRange(mapperCollection.Value.GetDependencies(property.Value, property.Key, flags));
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
                return CreateDocTypeDependency(item, flags);
            }

            return null;
        }

        protected static uSyncDependency CreateDocTypeDependency(IContentType item, DependencyFlags flags)
        {
            if (item != null)
            {
                new uSyncDependency
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


        protected JObject GetJsonValue(object value)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue)) return null;

            var jsonValue = JsonConvert.DeserializeObject<JObject>(stringValue);
            if (jsonValue == null) return null;

            return jsonValue;
        }

        protected IContentType GetDocType(JObject json, string alias)
        {
            if (json.ContainsKey(alias))
            {
                var docTypeAlias = json[alias].ToString();
                return GetDocType(docTypeAlias);
            }

            return default;
        }

        protected IContentType GetDocTypeByKey(JObject json, string keyAlias)
        {
            if (json.ContainsKey(keyAlias))
            {
                var attempt = json[keyAlias].TryConvertTo<Guid>();
                if (attempt.Success)
                {
                    return contentTypeService.Get(attempt.Result);
                }
            }

            return default;
        }

        protected IContentType GetDocType(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return default;
            return contentTypeService.Get(alias);
        }


    }
}
