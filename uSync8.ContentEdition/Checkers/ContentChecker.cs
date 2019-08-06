using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Checkers
{
    public class ContentChecker : ContentBaseChecker, ISyncDependencyChecker<IContent>
    {
        public ContentChecker(IEntityService entityService)
            : base(entityService, UmbracoObjectTypes.DocumentType)
        {
            ObjectType = UmbracoObjectTypes.Document;
        }

        public IEnumerable<uSyncDependency> GetDependencies(IContent item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.Content,
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
                dependencies.AddRange(GetParentDependencies(item.Id, DependencyOrders.Content - 1, flags));
            }

            if (flags.HasFlag(DependencyFlags.IncludeChildren))
            {
                dependencies.AddRange(GetChildDepencies(item.Id, DependencyOrders.Content + 1, flags));
            }

            return dependencies;
        }

    }
}
