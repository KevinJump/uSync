using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;
using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

namespace uSync8.Community.Contrib.Mappers
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

        public DocTypeGridMapper(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, contentTypeService, dataTypeService)
        { }

        public override string Name => "DocType Grid Mapper";

        public override string[] Editors => new string[] { "Umbraco.Grid.docType" };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var dteValue = GetValueAs<string>(value);
            if (string.IsNullOrEmpty(dteValue)) return Enumerable.Empty<uSyncDependency>();

            var jsonValue = JsonConvert.DeserializeObject<JObject>(dteValue);
            if (jsonValue == null) return Enumerable.Empty<uSyncDependency>();

            var docTypeAlias = jsonValue.Value<string>("dtgeContentTypeAlias");
            var docValue = jsonValue.Value<JObject>("value");

            if (docTypeAlias == null || docValue == null)
                return Enumerable.Empty<uSyncDependency>();

            var docType = contentTypeService.Get(docTypeAlias);
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
            dependencies.AddRange(GetPropertyDependencies(docValue, docType.CompositionPropertyTypes, flags));

            return dependencies;
        }

    }
}
