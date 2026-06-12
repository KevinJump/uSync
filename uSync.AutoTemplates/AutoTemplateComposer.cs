using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.AutoTemplates;

public class AutoTemplateComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // only load when the backoffice is enabled. 
        if (builder.Services.Any(s => s.ServiceType == typeof(IBackOfficeEnabledMarker)))
            builder.AdduSyncAutoTemplates();
    }
}
