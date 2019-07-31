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
    public class MediaTypeChecker :
        ContentTypeBaseChecker<IMediaType>,
        ISyncDependencyChecker<IMediaType>
    {
        public MediaTypeChecker(
            IEntityService entityService,
            IDataTypeService dataTypeService,
            ILocalizationService localizationService)
            : base(entityService, dataTypeService, localizationService)
        { }

        public override UmbracoObjectTypes ObjectType => UmbracoObjectTypes.MediaType;

        public IEnumerable<uSyncDependency> GetDependencies(IMediaType item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.MediaTypes,
                Flags = flags
            });

            if (!flags.HasFlag(DependencyFlags.NoDependencies))
            {
                dependencies.AddRange(CalcDataTypeDependencies(item, flags));
                dependencies.AddRange(CalcCompositions(item, DependencyOrders.MediaTypes - 1, flags));
            }

            dependencies.AddRange(CalcChildren(item.Id, flags));

            return dependencies;
        }
    }
}
