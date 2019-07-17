using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace uSync8.Core.Dependency
{
    public class ContentTypeChecker : 
        ContentTypeBaseChecker<IContentType>,
        ISyncDependencyChecker<IContentType>
    {

        public ContentTypeChecker(IDataTypeService dataTypeService)
            : base(dataTypeService)
        { }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.DocumentType;

        public IEnumerable<uSyncDependency> GetDependencies(IContentType item)
        {
            var dependencies = new List<uSyncDependency>();

            dependencies.Add(new uSyncDependency()
            {
                Udi = item.GetUdi(),
                Order = DependencyOrders.ContentTypes
            });

            dependencies.AddRange(CalcDataTypeDependencies(item)); ;
            dependencies.AddRange(CalcCompositions(item, DependencyOrders.ContentTypes - 1));
            dependencies.AddRange(CalcTemplateDependencies(item));
            return dependencies;
        }


        private IEnumerable<uSyncDependency> CalcTemplateDependencies(IContentType item)
        {
            var templates = new List<uSyncDependency>();

            foreach (var template in item.AllowedTemplates)
            {
                templates.Add(new uSyncDependency()
                {
                    Udi = template.GetUdi(),
                    Order = DependencyOrders.Templates
                });
            }

            return templates;
        }
    }
}
