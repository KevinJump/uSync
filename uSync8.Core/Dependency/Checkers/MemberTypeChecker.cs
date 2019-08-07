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
            IEntityService entityService,
            IDataTypeService dataTypeService, ILocalizationService localizationService) 
            : base(entityService, dataTypeService, localizationService)
        {
        }

        public override UmbracoObjectTypes ObjectType => UmbracoObjectTypes.MemberType;

        public IEnumerable<uSyncDependency> GetDependencies(IMemberType item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
                Udi = item.GetUdi(),
                Order = DependencyOrders.MemberTypes,
                Flags = flags,
                Level = item.Level
            });

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                dependencies.AddRange(CalcDataTypeDependencies(item, flags));
                dependencies.AddRange(CalcCompositions(item, DependencyOrders.MemberTypes - 1, flags));
            }

            dependencies.AddRange(CalcChildren(item.Id, flags));

            return dependencies;
        }
    }
}
