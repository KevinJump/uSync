using System;
using System.Collections.Generic;
using System.Linq;
using uSync.Core.Dependency;

namespace uSync.Core.Mapping
{
    public static class SyncValueMapperCollectionExtensions
    {
        public static IEnumerable<uSyncDependency> GetDependencies(
            this SyncValueMapperCollection mapperCollection,
            object value, string editorAlias, DependencyFlags flags)
        {
            var mappers = mapperCollection.GetSyncMappers(editorAlias);
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
