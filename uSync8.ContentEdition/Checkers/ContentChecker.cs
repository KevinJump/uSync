using System.Collections.Generic;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Checkers
{
    public class ContentChecker : ContentBaseChecker, ISyncDependencyChecker<IContent>
    {
        private readonly IContentService contentService;

        public ContentChecker(IEntityService entityService, 
            IContentService contentService,
            SyncValueMapperCollection mappers)
            : base(entityService, UmbracoObjectTypes.DocumentType, mappers)
        {
            this.contentService = contentService;

            ObjectType = UmbracoObjectTypes.Document;
        }

        public IEnumerable<uSyncDependency> GetDependencies(IContent item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
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

            // if we are getting linked items or media, include in the look up.
            if (flags.HasFlagAny(DependencyFlags.IncludeLinked | DependencyFlags.IncludeMedia | DependencyFlags.IncludeDependencies))
            { 
                dependencies.AddRange(GetPropertyDependencies(item, flags));
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

        protected override IContentBase GetItemById(int id)
            => contentService.GetById(id);
    }
}
