using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    public static class SyncValueMapperFactory
    {
        [Obsolete("Request all mappers and you can chain multiple mappers")]
        public static ISyncMapper GetMapper(string editorAlias)
        {
            return Current
                    .Factory
                    .GetInstance<SyncValueMapperCollection>()
                    .GetSyncMapper(editorAlias);
        }

        public static IEnumerable<ISyncMapper> GetMappers(string editorAlias)
        {
            return Current
                .Factory
                .GetInstance<SyncValueMapperCollection>()
                .GetSyncMappers(editorAlias);
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

        public static IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var mappers = GetMappers(editorAlias);
            if (!mappers.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();
            foreach(var mapper in mappers)
            {
                dependencies.AddRange(mapper.GetDependencies(value, editorAlias, flags));
            }
            return dependencies;
        }
    }

}
