using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;

namespace uSync.Core.Serialization;

public interface ISyncEntityContainerSerializer<TObject> : ISyncSerializer<TObject>
    where TObject : ITreeEntity
{
    /// <summary>
    ///  the type that the conainers contain - this is used to determine the path of the item, and to determine the root items
    /// </summary>
    UmbracoObjectTypes ContainedType { get; }

    /// <summary>
    ///  the path .
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    string GetEntityContainerPath(TObject item);
}