
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
        // uSync core will actually run when their is no back office loaded. 
        //if (builder.IsUmbracoBackOfficeEnabled() is false)
        //    return;
        
        builder.AdduSync();
    }
}
