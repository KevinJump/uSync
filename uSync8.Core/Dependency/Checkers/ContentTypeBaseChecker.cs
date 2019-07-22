using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace uSync8.Core.Dependency
{
    public class ContentTypeBaseChecker<TObject>
        where TObject : IContentTypeComposition
    {

        private readonly IDataTypeService dataTypeService;

        public ContentTypeBaseChecker(
            IDataTypeService dataTypeService)
        {
            this.dataTypeService = dataTypeService;
        }

        protected IEnumerable<uSyncDependency> CalcDataTypeDependencies(TObject item)
        {
            var dataTypes = new List<uSyncDependency>();

            foreach(var property in item.PropertyTypes)
            {
                var dataType = dataTypeService.GetDataType(property.DataTypeId);
                if (dataType != null)
                {
                    dataTypes.Add(new uSyncDependency()
                    {
                        Udi = dataType.GetUdi(),
                        Order = DependencyOrders.DataTypes
                    });
                }

                // TODO: Dictionary Item Dependencies, when labels start with #
            }

            return dataTypes;
        }


        protected IEnumerable<uSyncDependency> CalcCompositions(IContentTypeComposition item, int priority)
        {
            var contentTypes = new List<uSyncDependency>();

            foreach (var composition in item.ContentTypeComposition)
            {
                contentTypes.Add(new uSyncDependency()
                {
                    Udi = composition.GetUdi(),
                    Order = priority
                });

                if (composition.ContentTypeComposition != null && composition.ContentTypeComposition.Any())
                {
                    contentTypes.AddRange(CalcCompositions(composition, priority - 1));
                }
            }

            return contentTypes;
        }

    }
}
