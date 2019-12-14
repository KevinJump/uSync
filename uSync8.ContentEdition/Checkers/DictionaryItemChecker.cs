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
    public class DictionaryItemChecker : ISyncDependencyChecker<IDictionaryItem>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        // public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.DictionaryItem


        public IEnumerable<uSyncDependency> GetDependencies(IDictionaryItem item, DependencyFlags flags)
        {
            uSyncDependency.FireUpdate(item.ItemKey);

            var dependencies = new List<uSyncDependency>();
            dependencies.Add(new uSyncDependency()
            {
                Name = item.ItemKey,
                Order = DependencyOrders.DictionaryItems,
                Udi = item.GetUdi(),
                Flags = flags,
                Level = 0
            });

            return dependencies;
        }
    }
}
