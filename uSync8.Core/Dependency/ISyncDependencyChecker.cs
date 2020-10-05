using System.Collections.Generic;

using Umbraco.Core.Models;

namespace uSync8.Core.Dependency
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
