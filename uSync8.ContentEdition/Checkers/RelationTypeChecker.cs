using System.Collections.Generic;

using Umbraco.Core;
using Umbraco.Core.Models;

using uSync8.Core.Dependency;

namespace uSync.ContentEdition.Checkers
{
    public class RelationTypeChecker : ISyncDependencyChecker<IRelationType>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.RelationType;

        public IEnumerable<uSyncDependency> GetDependencies(IRelationType item, DependencyFlags flags)
        {
            return new uSyncDependency
            {
                Name = item.Name,
                Udi = item.GetUdi(),
                Order = DependencyOrders.RelationTypes,
                Flags = flags
            }.AsEnumerableOfOne();
        }
    }
}
