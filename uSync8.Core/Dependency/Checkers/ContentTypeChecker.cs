using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using static Umbraco.Core.Constants;

namespace uSync8.Core.Dependency
{
    public class ContentTypeChecker : 
        ContentTypeBaseChecker<IContentType>,
        ISyncDependencyChecker<IContentType>
    {

        public override UmbracoObjectTypes ObjectType => UmbracoObjectTypes.DocumentType;

        public ContentTypeChecker(IDataTypeService dataTypeService, ILocalizationService localizationService, IEntityService entityService)
            : base(entityService, dataTypeService, localizationService)
        {
        }


        public IEnumerable<uSyncDependency> GetDependencies(IContentType item, DependencyFlags flags)
        {
            uSyncDependency.FireUpdate(item.Name);

            var dependentflags = flags & ~DependencyFlags.IncludeChildren;

            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Name = item.Name,
                Udi = item.GetUdi(),
                Order = DependencyOrders.ContentTypes,
                Flags = dependentflags,
                Level = item.Level
            });


            if (flags.HasFlag(DependencyFlags.IncludeDependencies)) {
                dependencies.AddRange(CalcDataTypeDependencies(item, dependentflags)); ;
                dependencies.AddRange(CalcCompositions(item, DependencyOrders.ContentTypes - 1, dependentflags));
                dependencies.AddRange(CalcTemplateDependencies(item, dependentflags));
            }

            dependencies.AddRange(CalcChildren(item.Id, flags));

            return dependencies;
        }


        private IEnumerable<uSyncDependency> CalcTemplateDependencies(IContentType item, DependencyFlags flags)
        {
            var templates = new List<uSyncDependency>();

            if (flags.HasFlag(DependencyFlags.IncludeViews))
            {
                foreach (var template in item.AllowedTemplates)
                {
                    templates.Add(new uSyncDependency()
                    {
                        Name = item.Name,
                        Udi = template.GetUdi(),
                        Order = DependencyOrders.Templates,
                        Flags = flags,
                        Level = template.Path.ToDelimitedList().Count()
                    });
                }
            }

            return templates;
        }

    }
}
