﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
{
    public class NestedContentMapper : SyncNestedJsonValueMapperBase, ISyncMapper
    {
        private readonly string docTypeAliasValue = NestedContentPropertyEditor.ContentTypeAliasPropertyKey;
        
        public NestedContentMapper(
            IEntityService entityService,
            Lazy<SyncValueMapperCollection> mapperCollection,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, mapperCollection, contentTypeService, dataTypeService)
        { }

        public override string Name => "Nested Content Mapper";

        public override string[] Editors => new string[] {
            "Our.Umbraco.NestedContent",
            Constants.PropertyEditors.Aliases.NestedContent
        };

        protected override string ProcessValues(JToken jsonValue, string editorAlias, Func<JObject, IContentType, JObject> GetPropertiesMethod)
        {
            if (jsonValue is JArray nestedJson)
            {
                foreach (var item in nestedJson.Cast<JObject>())
                {
                    var docType = GetDocType(item, this.docTypeAliasValue);
                    if (docType == null) continue;

                    GetPropertiesMethod(item, docType);
                }
                return JsonConvert.SerializeObject(nestedJson, Formatting.Indented);
            }

            return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson())
                return Enumerable.Empty<uSyncDependency>();

            var nestedJson = JsonConvert.DeserializeObject<JArray>(stringValue);
            if (nestedJson == null || !nestedJson.Any())
                return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var item in nestedJson.Cast<JObject>())
            {
                var docTypeAlias = item[this.docTypeAliasValue].ToString();
                var docType = GetDocType(docTypeAlias);
                if (docType == null) continue;

                if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                {
                    var docTypeDep = CreateDocTypeDependency(docTypeAlias, flags);
                    if (docTypeDep != null)
                        dependencies.Add(docTypeDep);
                }

                dependencies.AddRange(GetPropertyDependencies(item, docType, flags));
            }

            return dependencies;
        }
    }
}

