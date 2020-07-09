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
    {
        IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags);
    }
}
