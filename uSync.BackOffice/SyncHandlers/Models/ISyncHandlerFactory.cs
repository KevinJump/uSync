using System.Collections.Generic;

using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice.SyncHandlers.Models;

/// <summary>
///  Factory for accessing the handlers and their configuration
/// </summary>
public interface ISyncHandlerFactory
{
    /// <summary>
    ///  Name of the default handler set
    /// </summary>
    string DefaultSet { get; }

    /// <summary>
    ///  Get all handlers
    /// </summary>
    IEnumerable<ISyncHandler> GetAll();

    /// <summary>
    ///  Get Default Handlers based on Alias
    /// </summary>
    /// <param name="aliases">aliases of handlers you want </param>
    /// <returns>Handler/Config Pair with default config loaded</returns>
    IEnumerable<HandlerConfigPair> GetDefaultHandlers(IEnumerable<string> aliases);
   
    /// <summary>
    ///  returns the handler groups (settings, content, users, etc) that stuff can be grouped into
    /// </summary>
    IEnumerable<string> GetGroups();

    /// <summary>
    ///  Get a handler by alias 
    /// </summary>
    ISyncHandler? GetHandler(string alias);

    /// <summary>
    ///  Get all handlers that match the list of names in the aliases array 
    /// </summary>
    IEnumerable<ISyncHandler> GetHandlers(params string[] aliases);
    
    /// <summary>
    ///  Get the valid (by config) handler groups available to this setup
    /// </summary>
    IEnumerable<string> GetValidGroups(SyncHandlerOptions? options = null);
    
    /// <summary>
    ///  Get a valid handler (based on config) by alias and options
    /// </summary>
    HandlerConfigPair? GetValidHander<TObject>(SyncHandlerOptions? options = null);

    /// <summary>
    ///  Get a valid handler (based on config) by options
    /// </summary>
    HandlerConfigPair? GetValidHandler(string alias, SyncHandlerOptions? options = null);
    
    /// <summary>
    ///  Get a valid handler (based on config) by Umbraco Entity Type and options
    /// </summary>
    HandlerConfigPair? GetValidHandlerByEntityType(string entityType, SyncHandlerOptions? options = null);
    
    /// <summary>
    ///  Get a valid handler (based on config) by ItemType and options
    /// </summary>
    HandlerConfigPair? GetValidHandlerByTypeName(string itemType, SyncHandlerOptions? options = null);
    
    /// <summary>
    ///  get the handler groups and their icons
    /// </summary>
    /// <remarks>
    ///  if we don't have a defined icon for a group, the icon from the first handler in the group
    ///  will be used. 
    /// </remarks>
    IDictionary<string, string> GetValidHandlerGroupsAndIcons(SyncHandlerOptions? options = null);

    /// <summary>
    /// Get all valid (by configuration) handlers that fulfill the criteria set out in the passed SyncHandlerOptions 
    /// </summary>
    IEnumerable<HandlerConfigPair> GetValidHandlers(SyncHandlerOptions? options = null);

    /// <summary>
    ///  get a collection of valid handlers that match the list of aliases 
    /// </summary>
    IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions? options = null);
    
    /// <summary>
    ///  Get a all valid handlers (based on config) that can handle a given entityType
    /// </summary>
    IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions? options = null);
}