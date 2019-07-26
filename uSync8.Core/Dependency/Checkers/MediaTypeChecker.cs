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
            IDataTypeService dataTypeService, ILocalizationService localizationService)
            : base(dataTypeService, localizationService)
        { }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.MediaType;

        public IEnumerable<uSyncDependency> GetDependencies(IMediaType item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.MediaTypes,
                Flags = flags
            });

            if (flags.HasFlag(DependencyFlags.NoDependencies)) return dependencies;

            dependencies.AddRange(CalcDataTypeDependencies(item, flags));
            dependencies.AddRange(CalcCompositions(item, DependencyOrders.MediaTypes - 1, flags));

            return dependencies;
        }
    }
}
