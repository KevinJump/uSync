using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
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
            if (GuidUdi.TryParse(udiString, out GuidUdi udi))
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
            // TODO: We are doing a get here, just to get somethings 'name' - can we live without the name? 
            // if (udi != null) return entityService.Get(udi.Guid);

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
