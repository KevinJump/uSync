using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace uSync8.Core.Dependency
{
    public class MemberTypeChecker : ContentTypeBaseChecker<IMemberType>,
        ISyncDependencyChecker<IMemberType>
    {
        public MemberTypeChecker(
            IDataTypeService dataTypeService) 
            : base(dataTypeService)
        {
        }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.MemberType;

        public IEnumerable<uSyncDependency> GetDependencies(IMemberType item)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.MemberTypes
            });

            dependencies.AddRange(CalcDataTypeDependencies(item));
            dependencies.AddRange(CalcCompositions(item, DependencyOrders.MemberTypes - 1));
            return dependencies;
        }
    }
}
