using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core;

/// we are wrapping and extending Udi's 
/// the internal functions throw if you don't know the type
/// so here we capture and add return Unknown.
public static class uSyncObjectType
{
    public const string File = "physical-file";

    public static UmbracoObjectTypes ToUmbracoObjectType(string entityType)
    {
        try
        {
            return UdiEntityTypeHelper.ToUmbracoObjectType(entityType);
        }
        catch (NotSupportedException)
        {
            // this gets thrown, when its not a known type, but for 
            // use we want to carry on with Unknown
            return UmbracoObjectTypes.Unknown;
        }
    }

    /// <summary>
    ///  Get the type of container that is used for this entity type.
    /// </summary>
    public static UmbracoObjectTypes ToContainerUmbracoObjectType(string entityType)
    {
        return entityType switch
        {
            UdiEntityType.DocumentType => UmbracoObjectTypes.DocumentTypeContainer,
            UdiEntityType.MediaType => UmbracoObjectTypes.MediaTypeContainer,
            UdiEntityType.DataType => UmbracoObjectTypes.DataTypeContainer,
            _ => UmbracoObjectTypes.Unknown,
        };
    }
}
