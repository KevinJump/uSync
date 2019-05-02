using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

namespace uSync8.ContentEdition.Mappers
{
    public class SyncValueMapperCollection
            : BuilderCollectionBase<ISyncMapper>
    {

        public SyncValueMapperCollection(IEnumerable<ISyncMapper> items)
            : base(items) { }

        public ISyncMapper GetSyncMapper(string alias)
        {
            return this.FirstOrDefault(x => x.Editors.InvariantContains(alias));
        }

        public ISyncMapper GetSyncMapper(PropertyType propertyType)
        {
            return this.FirstOrDefault(x => x.IsMapper(propertyType));
        }

        public string GetExportValue(object value, string editorAlias)
        {
            var mapper = GetSyncMapper(editorAlias);
            if (mapper == null) return value.ToString();

            return mapper.GetExportValue(value, editorAlias);
        }

        public object GetImportValue(string value, string editorAlias)
        {
            var mapper = GetSyncMapper(editorAlias);
            if (mapper == null) return value;

            return mapper.GetImportValue(value, editorAlias);
        }
    }

    public class SyncValueMapperCollectionBuilder
        : LazyCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    {
        protected override SyncValueMapperCollectionBuilder This => this;
    }
}
