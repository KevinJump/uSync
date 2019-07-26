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
        private readonly ILocalizationService localizationService;

        public ContentTypeBaseChecker(
            IDataTypeService dataTypeService,
            ILocalizationService localizationService)
        {
            this.dataTypeService = dataTypeService;
            this.localizationService = localizationService;
        }

        protected IEnumerable<uSyncDependency> CalcDataTypeDependencies(TObject item, DependencyFlags flags)
        {
            var dataTypes = new List<uSyncDependency>();

            var dictionaryKeys = new List<string>();

            foreach(var property in item.PropertyTypes)
            {
                var dataType = dataTypeService.GetDataType(property.DataTypeId);
                if (dataType != null)
                {
                    dataTypes.Add(new uSyncDependency()
                    {
                        Udi = dataType.GetUdi(),
                        Order = DependencyOrders.DataTypes,
                        Flags = flags
                    });
                }

                // TODO: Dictionary Item Dependencies, when labels start with #
                if (property.Name.StartsWith("#"))
                {
                    dictionaryKeys.Add(property.Name.Substring(1));
                }
            }

            foreach(var tab in item.PropertyGroups)
            {
                if (tab.Name.StartsWith("#"))
                    dictionaryKeys.Add(tab.Name.Substring(1));
            }

            dataTypes.AddRange(GetDictionaryDependencies(dictionaryKeys, flags));

            return dataTypes;
        }

        private IEnumerable<uSyncDependency> GetDictionaryDependencies(IEnumerable<string> keys, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            foreach(var key in keys)
            {
                var item = localizationService.GetDictionaryItemByKey(key);
                if (item != null)
                {

                    dependencies.Add(new uSyncDependency()
                    {
                        Flags = flags,
                        Order = DependencyOrders.DictionaryItems,
                        Udi = item.GetUdi() // strong chance you can't do this with a dictionary item :( 
                    });
                }
            }

            return dependencies;
        }

        protected IEnumerable<uSyncDependency> CalcCompositions(IContentTypeComposition item, int priority, DependencyFlags flags)
        {
            var contentTypes = new List<uSyncDependency>();

            foreach (var composition in item.ContentTypeComposition)
            {
                contentTypes.Add(new uSyncDependency()
                {
                    Udi = composition.GetUdi(),
                    Order = priority,
                    Flags = flags
                });

                if (composition.ContentTypeComposition != null && composition.ContentTypeComposition.Any())
                {
                    contentTypes.AddRange(CalcCompositions(composition, priority - 1, flags));
                }
            }

            return contentTypes;
        }

    }
}
