using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Composing;

using uSync.Core.Dependency;
using Umbraco.Core.Composing;

namespace uSync.ContentEdition.Mapping
{
    public class SyncValueMapperFactory
    {
        private readonly IFactory factory;

        public SyncValueMapperFactory(IFactory factory)
        {
            this.factory = factory;
        }

        public IEnumerable<ISyncMapper> GetMappers(string editorAlias)
        {
            return factory
                .GetInstance<SyncValueMapperCollection>()
                .GetSyncMappers(editorAlias);
        }

        public string GetExportValue(object value, string editorAlias)
        {
            return factory
                .GetInstance<SyncValueMapperCollection>()
                .GetExportValue(value, editorAlias);
        }

        public object GetImportValue(string value, string editorAlias)
        {
            return factory
                .GetInstance<SyncValueMapperCollection>()
                .GetImportValue(value, editorAlias);
        }

        public IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var mappers = GetMappers(editorAlias);
            if (!mappers.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();
            foreach (var mapper in mappers)
            {
                dependencies.AddRange(mapper.GetDependencies(value, editorAlias, flags));
            }
            return dependencies;
        }
    }

}
