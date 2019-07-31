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
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.ContentTypes,
                Flags = flags
            });


            if (!flags.HasFlag(DependencyFlags.NoDependencies)) {
                dependencies.AddRange(CalcDataTypeDependencies(item, flags)); ;
                dependencies.AddRange(CalcCompositions(item, DependencyOrders.ContentTypes - 1, flags));
                dependencies.AddRange(CalcTemplateDependencies(item, flags));
            }

            dependencies.AddRange(CalcChildren(item.Id, flags));

            return dependencies;
        }


        private IEnumerable<uSyncDependency> CalcTemplateDependencies(IContentType item, DependencyFlags flags)
        {
            var templates = new List<uSyncDependency>();

            if (!flags.HasFlag(DependencyFlags.NoTemplates))
            {
                foreach (var template in item.AllowedTemplates)
                {
                    templates.Add(new uSyncDependency()
                    {
                        Udi = template.GetUdi(),
                        Order = DependencyOrders.Templates,
                        Flags = flags
                    });
                }
            }

            return templates;
        }

    }
}
