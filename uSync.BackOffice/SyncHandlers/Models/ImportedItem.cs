using System.Xml.Linq;

namespace uSync.BackOffice.SyncHandlers.Models;

/// <summary>
///  represents an item and its accompanying xml. 
/// </summary>
public class ImportedItem<TObject>(XElement node, TObject item)
{
    /// <summary>
    ///  xml node used for import / reporting
    /// </summary>
    public XElement Node { get; set; } = node;

    /// <summary>
    ///  concrete item that is updated/saved
    /// </summary>
    public TObject Item { get; set; } = item;
}
