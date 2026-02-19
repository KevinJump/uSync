using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Text;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.Extend.Example;

public class MyCustomComposer : IComposer   
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IMyCustomObjectService, MyCustomObjectService>();
    }
}
