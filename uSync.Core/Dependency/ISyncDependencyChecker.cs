using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

namespace uSync.Core.Dependency
{
    public interface ISyncDependencyItem
    {
        UmbracoObjectTypes ObjectType { get; }
    }

    public interface ISyncDependencyChecker<TObject> : ISyncDependencyItem
    {
        IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags);
    }
}
