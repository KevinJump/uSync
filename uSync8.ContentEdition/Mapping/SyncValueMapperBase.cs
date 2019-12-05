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
            if (udiStrings == null || !udiStrings.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var udiString in udiStrings)
            {
                var dependency = CreateDependency(udiString, flags);
                if (dependency != null)
                    dependencies.Add(dependency);
            }

            return dependencies;
        }

        protected uSyncDependency CreateDependency(string udiString, DependencyFlags flags)
        {
            if (Udi.TryParse(udiString, out Udi udi))
            {
                return CreateDependency(udi, flags);
            }

            return null;
        }

        protected uSyncDependency CreateDependency(Udi udi, DependencyFlags flags)
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

        private IEntitySlim GetElement(Udi udi)
        {
            if (udi is GuidUdi guidUdi)
            {
                return entityService.Get(guidUdi.Guid);
            }
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
