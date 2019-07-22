using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Dependency
{
    public interface ISyncDependencyItem
    {
        UmbracoObjectTypes ObjectType { get; }
    }

    public interface ISyncDependencyChecker<TObject> : ISyncDependencyItem
        where TObject : IEntity
    {
        IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags);
    }

    [FlagsAttribute]
    public enum DependencyFlags
    {
        None = 0,
        IncludeChildren = 2,
        IncludeAncestors = 4,
        NoDependencies = 8
    }
}
