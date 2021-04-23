using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
{
    public abstract class SyncValueMapperBase
    {
        protected readonly IEntityService entityService;

        public SyncValueMapperBase(IEntityService entityService)
        {
            this.entityService = entityService;
        }

        public abstract string Name { get; }

        public abstract string[] Editors { get; }

        public virtual bool IsMapper(PropertyType propertyType)
            => Editors.InvariantContains(propertyType.PropertyEditorAlias);

        public virtual IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
            => Enumerable.Empty<uSyncDependency>();

        public virtual string GetExportValue(object value, string editorAlias)
            => value.ToString();

        public virtual string GetImportValue(string value, string editorAlias)
            => value;



        protected IEnumerable<uSyncDependency> CreateDependencies(IEnumerable<string> udiStrings, DependencyFlags flags)
        {
            if (udiStrings == null || !udiStrings.Any()) yield break;

            foreach (var udiString in udiStrings)
            {
                var dependency = CreateDependency(udiString, flags);
                if (dependency != null) yield return dependency;
            }
        }

        protected uSyncDependency CreateDependency(string udiString, DependencyFlags flags)
        {
            if (UdiParser.TryParse<GuidUdi>(udiString, out GuidUdi udi))
            {
                return CreateDependency(udi, flags);
            }

            return null;
        }

        protected uSyncDependency CreateDependency(GuidUdi udi, DependencyFlags flags)
        {
            var entity = GetElement(udi);

            return new uSyncDependency()
            {
                Name = entity == null ? udi.ToString() : entity.Name,
                Udi = udi,
                Flags = flags,
                Order = DependencyOrders.OrderFromEntityType(udi.EntityType),
                Level = entity == null ? 0 : entity.Level
            };
        }

        private IEntitySlim GetElement(GuidUdi udi)
        {
            if (udi != null)
                return entityService.Get(udi.Guid);

            return null;
        }

        /// <summary>
        ///  helper to convert object to a string (with all the checks)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected TObject GetValueAs<TObject>(object value)
        {
            if (value == null) return default;
            var attempt = value.TryConvertTo<TObject>();
            if (!attempt) return default;

            return attempt.Result;
        }
    }
}
