using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Checkers
{
    [Obsolete("Checkers are now handled outside of the uSync Core")]
    public abstract class ContentBaseChecker
    {
        protected readonly IEntityService entityService;
        private UmbracoObjectTypes contentTypeObjectType;
        private SyncValueMapperCollection mappers;

        public UmbracoObjectTypes ObjectType { get; protected set; } = UmbracoObjectTypes.Unknown;

        public ContentBaseChecker(IEntityService entityService,
            UmbracoObjectTypes contentTypeObjectType,
            SyncValueMapperCollection mappers)
        {
            this.entityService = entityService;
        }

        protected uSyncDependency CalcDocTypeDependency(IContentBase item, DependencyFlags flags)
            => null;

        protected IEnumerable<uSyncDependency> GetParentDependencies(int id, int order, DependencyFlags flags)
            => Enumerable.Empty<uSyncDependency>();

        protected IEnumerable<uSyncDependency> GetChildDepencies(int id, int order, DependencyFlags flags, int min, int max)
            => Enumerable.Empty<uSyncDependency>();

        protected abstract IContentBase GetItemById(int id);
        protected readonly string[] settingsTypes = Array.Empty<string>();

        protected IEnumerable<uSyncDependency> GetPropertyDependencies(IContentBase item, DependencyFlags flags)
            => Enumerable.Empty<uSyncDependency>();

    }
}