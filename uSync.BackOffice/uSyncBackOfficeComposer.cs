
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.BackOffice
{
    public class uSyncBackOfficeComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // the composers add uSync, but the extension methods
            // will only add the values if uSync hasn't already 
            // been added, so you can for example add uSync to your
            // startup.cs file. and then the composers don't fire
            builder.AdduSync();
        }
    }
}
