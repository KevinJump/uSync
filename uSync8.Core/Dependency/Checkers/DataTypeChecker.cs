using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace uSync8.Core.Dependency
{
    public class DataTypeChecker : ISyncDependencyChecker<IDataType>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.DataType;

        public IEnumerable<uSyncDependency> GetDependencies(IDataType item, DependencyFlags flags)
        {
            uSyncDependency.FireUpdate(item.Name);

            var dependencies = new List<uSyncDependency>();
            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
                Order = DependencyOrders.DataTypes,
                Udi = item.GetUdi(),
                Flags = flags,
                Level = item.Level
            });

            // what can datatypes depend on? 

            return dependencies;
        }

    }
}
