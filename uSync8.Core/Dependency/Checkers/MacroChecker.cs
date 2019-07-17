using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace uSync8.Core.Dependency
{
    public class MacroChecker : ISyncDependencyChecker<IMacro>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        public IEnumerable<uSyncDependency> GetDependencies(IMacro item)
        {
            var dependencies = new List<uSyncDependency>
            {
                new uSyncDependency()
                {
                    Udi = item.GetUdi(),
                    Order = DependencyOrders.Macros
                }
            };

            return dependencies;
        }
    }
}
