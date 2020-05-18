using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models.Entities;
using uSync8.Core.Models;

namespace uSync8.Core.Tracking
{
    /// <summary>
    ///  Sync Tracker - Returns a list of changes for a given XML Element 
    /// </summary>
    /// <remarks>
    ///  Tracking are expensive operations, so we only call tracker when 
    ///  we know that there are changes (through the serializers IsCurrent method
    /// </remarks>
    public interface ISyncTracker<TObject>
        where TObject : IEntity
    {
        /// <summary>
        ///  Get details of the changes in this XML vs what is in Umbraco.
        /// </summary>
        IEnumerable<uSyncChange> GetChanges(XElement node);
    }
}
