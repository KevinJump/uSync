using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;


namespace uSync8.ContentEdition.Mappers
{
    public static class SyncValueMapperFactory
    {
        public static ISyncMapper GetMapper(string editorAlias)
        {
            return Current
                    .Factory
                    .GetInstance<SyncValueMapperCollection>()
                    .GetSyncMapper(editorAlias);
        }
        public static string GetExportValue(object value, string editorAlias)
        {
            return Current
                    .Factory
                    .GetInstance<SyncValueMapperCollection>()
                    .GetExportValue(value, editorAlias);
        }

        public static object GetImportValue(string value, string editorAlias)
        {
            return Current
                    .Factory
                    .GetInstance<SyncValueMapperCollection>()
                    .GetImportValue(value, editorAlias);
        }
    }

}
