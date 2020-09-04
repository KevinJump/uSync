using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;
using uSync.ContentEdition.Mapping;
using uSync.Core.Dependency;

namespace uSync.Community.Contrib.Mappers
{
    /// <summary>
    ///  value/dependency mapper for DocTypeGridEditor.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   In genral for Umbraco 8 we don't need Value mappers, because
    ///   everything is Guid based
    ///  </para>
    ///  <para>
    ///   More relavent are the dependency finding functions, as these
    ///   will help uSync.Publisher / Exporter find out what linked
    ///   media/content and doctypes are needed to render your DTGE 
    ///   in another site. 
    ///  </para>
    ///  <para>
    ///   this mapper is more complicated than most need to be because
    ///   DTGE stores other content types within it, so we have to loop
    ///   into them and call the mappers for all the properties contained
    ///   within. Most of the time for simple mappers you don't need to 
    ///   do this. 
    ///  </para>
    /// </remarks>
    public class DocTypeGridMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        private readonly string docTypeAliasValue = "dtgeContentTypeAlias";

        public DocTypeGridMapper(IEntityService entityService,
            SyncValueMapperFactory mapperFactory,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, mapperFactory, contentTypeService, dataTypeService)
        { }

        public override string Name => "DocType Grid Mapper";

        public override string[] Editors => new string[] { "Umbraco.Grid.docType", "Umbraco.Grid.doctypegrideditor" };

        /// <summary>
        ///  Get any formatted export values. 
        /// </summary>
        /// <remarks>
        ///  for 99% of properties you don't need to go in and get the 
        ///  potential internal values, but we do this on export because
        ///  we want to ensure we trigger formatting of Umbraco.DateTime values
        /// </remarks>
        public override string GetExportValue(object value, string editorAlias)
        {
            if (value == null) return string.Empty;

            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return value.ToString();

            var docType = GetDocType(jsonValue, this.docTypeAliasValue);
            if (docType == null) return value.ToString();

            // jarray of values 
            var docValue = jsonValue.Value<JObject>("value");
            if (docValue == null) return value.ToString();

            GetExportProperties(docValue, docType);

            return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var jsonValue = GetJsonValue(value);
            if (value == null || jsonValue == null) return Enumerable.Empty<uSyncDependency>();

            var docValue = jsonValue.Value<JObject>("value");
            var docTypeAlias = jsonValue.Value<string>(this.docTypeAliasValue);
            if (docValue == null || docTypeAlias == null) return Enumerable.Empty<uSyncDependency>();

            var docType = GetDocType(docTypeAlias);
            if (docType == null) return Enumerable.Empty<uSyncDependency>();

            List<uSyncDependency> dependencies = new List<uSyncDependency>();

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                // get the docType as a dependency. 
                // you only need to get the primary doctype, a subsequent check
                // will get the full dependency tree for this doctype if it
                // is needed. 
                var docDependency = CreateDocTypeDependency(docTypeAlias, flags);
                if (docDependency != null)
                    dependencies.Add(docDependency);
            }

            // let the base class go through the PropertyTypes 
            // and call the mappers for each value, this gets us 
            // any internal dependencies (like media, etc) 
            // from within the content. 
            dependencies.AddRange(GetPropertyDependencies(docValue, docType, flags));

            return dependencies;
        }

    }
}
