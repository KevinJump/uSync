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
            var dependencies = new List<uSyncDependency>();
            dependencies.Add(new uSyncDependency()
            {
                Order = DependencyOrders.DataTypes,
                Udi = item.GetUdi()
            });

            // what can datatypes depend on? 

            return dependencies;
        }
    }
}
