using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using static Umbraco.Core.Constants;

namespace uSync8.Core
{   
    /// we are wrapping and extending Udi's 
    /// the internal functions throw if you don't know the type
    /// so here we capture and add...
    /// </summary>
    public static class uSyncObjectType 
    {
        public const string File = "physical-file";


        public static UmbracoObjectTypes ToUmbracoObjectType(string entityType)
        {
            try
            {
                return UdiEntityType.ToUmbracoObjectType(entityType);
            }
            catch(NotSupportedException ex)
            {
                // this gets thrown, when its not a known type, but for 
                // use we want to carry on with Unknown
                return UmbracoObjectTypes.Unknown;
            }
        }

        public static UmbracoObjectTypes ToContainerUmbracoObjectType(string entityType)
        {
            switch(entityType)
            {
                case UdiEntityType.DocumentType:
                    return UmbracoObjectTypes.DocumentTypeContainer;
                case UdiEntityType.MediaType:
                    return UmbracoObjectTypes.MediaTypeContainer;
                case UdiEntityType.DataType:
                    return UmbracoObjectTypes.DataTypeContainer;
                default:
                    return UmbracoObjectTypes.Unknown;
            }
        }
    }
}
