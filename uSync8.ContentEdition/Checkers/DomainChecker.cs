using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Checkers
{
    public class DomainChecker : ISyncDependencyChecker<IDomain>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        public IEnumerable<uSyncDependency> GetDependencies(IDomain item, DependencyFlags flags)
        {
            uSyncDependency.FireUpdate(item.DomainName);

            var dependencies = new List<uSyncDependency>();
            dependencies.Add(new uSyncDependency()
            {
                Name = item.DomainName,
                Order = DependencyOrders.Domain,
                Udi = new GuidUdi("domain", item.Key),
                Flags = flags,
                Level = 0
            });

            return dependencies;
        }
    }
}
