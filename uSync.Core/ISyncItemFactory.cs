using System.Xml.Linq;

using uSync.Core.Cache;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core;

/// <summary>
///  Factory for getting changes or dependencies from an item.
/// </summary>
public interface ISyncItemFactory
{
    /// <summary>
    ///  get all the serializers for a type of object
    /// </summary>
    IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>();

    /// <summary>
    ///  get a serializer by name
    /// </summary>
    ISyncSerializer<TObject>? GetSerializer<TObject>(string name);

    /// <summary>
    ///  get all the Sync trackers for a type of object
    /// </summary>
    IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>();

    /// <summary>
    ///  get all the changes for a node compared to the current item in umbraco. 
    /// </summary>
    Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, SyncSerializerOptions options);

    /// <summary>
    ///  get all the changes between two xml elements, based on the item type
    /// </summary>
    Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options);


    /// <summary>
    ///  get all the dependency checkers for a type of object
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>();

    /// <summary>
    ///  get all the possible dependencies for an object (based on the passed flags)
    /// </summary>
    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync<TObject>(TObject item, DependencyFlags flags);

    /// <summary>
    ///  an entity cache - can be used to improve lookup times on large syncs. 
    /// </summary>
    SyncEntityCache EntityCache { get; }
}