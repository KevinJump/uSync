using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;

using uSync8.BackOffice.Configuration;
using uSync8.Core.Dependency;
using uSync8.Core.Models;

namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  A Extended Handler, lets you do things to just one item, 
    ///  like import/export it, or work out what dependencies it has. 
    /// </summary>
    public interface ISyncExtendedHandler : ISyncHandler
    {
        string Group { get; }

        string EntityType { get; }
        string TypeName { get; }

        IEnumerable<uSyncAction> Import(string file, HandlerSettings settings, bool force);
        IEnumerable<uSyncAction> Report(string file, HandlerSettings settings);
        IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings);
        IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings);

        SyncAttempt<XElement> GetElement(Udi udi);

        IEnumerable<uSyncAction> ImportElement(XElement element, bool force);
        IEnumerable<uSyncAction> ReportElement(XElement element);

        IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags);
        IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags);
    }
}
