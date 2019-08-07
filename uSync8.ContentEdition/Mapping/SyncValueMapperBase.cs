using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    public abstract class SyncValueMapperBase
    {
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
            return new uSyncDependency()
            {
                Udi = udi,
                Flags = flags,
                Order = DependencyOrders.OrderFromEntityType(udi.EntityType)
            };
        }
    }
}
