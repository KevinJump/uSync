using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace uSync.Core.Documents;

internal class SyncDocumentUrlCleaner : ISyncDocumentUrlCleaner
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IDocumentUrlRepository _documentUrlRepository;
    private readonly IDocumentUrlAliasRepository _documentUrlAliasRepository;
    private readonly ILogger<SyncDocumentUrlCleaner> _logger;

    public SyncDocumentUrlCleaner(
        IDocumentUrlRepository documentUrlRepository,
        IDocumentUrlAliasRepository documentUrlAliasRepository,
        ILogger<SyncDocumentUrlCleaner> logger,
        ICoreScopeProvider scopeProvider)
    {
        _documentUrlRepository = documentUrlRepository;
        _documentUrlAliasRepository = documentUrlAliasRepository;
        _logger = logger;
        _scopeProvider = scopeProvider;
    }

    public void CleanUrlsForDocument(Guid key)
    {
        try
        {
            using (_scopeProvider.CreateCoreScope(autoComplete: true))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Cleaning urls for document {DocumentKey}", key);

                _documentUrlRepository.DeleteByDocumentKey([key]);

                // umbracoUrlAlias rows are stored by document key too, so the old
                // key's rows are left behind when the key changes.
                _documentUrlAliasRepository.DeleteByDocumentKey([key]);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error cleaning urls for document {DocumentKey}", key);
        }  
    }
}
