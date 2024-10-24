using System.Collections.Generic;

using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.SyncHandlers;
public interface ISyncHandlerFactory
{
    string DefaultSet { get; }

    IEnumerable<ISyncHandler> GetAll();
    IEnumerable<HandlerConfigPair> GetDefaultHandlers(IEnumerable<string> aliases);
    IEnumerable<string> GetGroups();
    ISyncHandler? GetHandler(string alias);
    IEnumerable<ISyncHandler> GetHandlers(params string[] aliases);
    IEnumerable<string> GetValidGroups(SyncHandlerOptions? options = null);
    HandlerConfigPair? GetValidHander<TObject>(SyncHandlerOptions? options = null);
    HandlerConfigPair? GetValidHandler(string alias, SyncHandlerOptions? options = null);
    HandlerConfigPair? GetValidHandlerByEntityType(string entityType, SyncHandlerOptions? options = null);
    HandlerConfigPair? GetValidHandlerByTypeName(string itemType, SyncHandlerOptions? options = null);
    IDictionary<string, string> GetValidHandlerGroupsAndIcons(SyncHandlerOptions? options = null);
    IEnumerable<HandlerConfigPair> GetValidHandlers(SyncHandlerOptions? options = null);
    IEnumerable<HandlerConfigPair> GetValidHandlers(string[] aliases, SyncHandlerOptions? options = null);
    IEnumerable<HandlerConfigPair> GetValidHandlersByEntityType(IEnumerable<string> entityTypes, SyncHandlerOptions? options = null);
}