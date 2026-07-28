
using Microsoft.Extensions.Logging;

using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using uSync.Core.Extensions;

namespace uSync.BackOffice;

/// <summary>
///  default composer to startup uSync
/// </summary>
public class uSyncBackOfficeComposer : IComposer
{
    /// <inheritdoc/>
    public void Compose(IUmbracoBuilder builder)
    {
        if (builder.IsUmbracoBackOfficeEnabled() is true)
        {
            builder.AdduSync();
        }
    }
}
