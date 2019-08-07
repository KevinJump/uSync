using System.Collections.Generic;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Checkers
{
    public class MediaChecker : ContentBaseChecker, ISyncDependencyChecker<IMedia>
    {
        public MediaChecker(IEntityService entityService, SyncValueMapperCollection mappers)
            : base(entityService, UmbracoObjectTypes.MediaType, mappers)
        {
            ObjectType = UmbracoObjectTypes.Media;
        }

        public IEnumerable<uSyncDependency> GetDependencies(IMedia item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
                Udi = item.GetUdi(),
                Order = DependencyOrders.Media,
                Flags = flags,
                Level = item.Level
            });

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                var contentType = CalcDocTypeDependency(item, flags);
                if (contentType != null)
                    dependencies.Add(contentType);
            }

            if (flags.HasFlag(DependencyFlags.IncludeAncestors))
            {
                dependencies.AddRange(GetParentDependencies(item.Id, DependencyOrders.Media - 1, flags));
            }

            if (flags.HasFlag(DependencyFlags.IncludeChildren))
            {
                dependencies.AddRange(GetChildDepencies(item.Id, DependencyOrders.Media + 1 , flags));
            }

            return dependencies;
        }
    }
}
