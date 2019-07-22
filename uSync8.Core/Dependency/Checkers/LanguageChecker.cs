using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace uSync8.Core.Dependency
{
    public class LanguageChecker : ISyncDependencyChecker<ILanguage>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Language;

        public IEnumerable<uSyncDependency> GetDependencies(ILanguage item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>
            {
                new uSyncDependency()
                {
                    Udi = item.GetUdi(),
                    Order = DependencyOrders.Languages
                }
            };

            return dependencies;
               
        }
    }
}
