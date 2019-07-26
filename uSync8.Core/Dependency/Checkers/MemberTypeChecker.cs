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
            IDataTypeService dataTypeService, ILocalizationService localizationService) 
            : base(dataTypeService, localizationService)
        {
        }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.MemberType;

        public IEnumerable<uSyncDependency> GetDependencies(IMemberType item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.MemberTypes,
                Flags = flags
            });

            if (flags.HasFlag(DependencyFlags.NoDependencies)) return dependencies;

            dependencies.AddRange(CalcDataTypeDependencies(item, flags));
            dependencies.AddRange(CalcCompositions(item, DependencyOrders.MemberTypes - 1, flags));
            return dependencies;
        }
    }
}
