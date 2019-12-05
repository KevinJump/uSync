using Umbraco.Core;
using Umbraco.Core.Composing;


namespace uSync8.ContentEdition.Mapping
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
