using System;
using System.Linq;

using Umbraco.Cms.Core.Logging.Viewer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.BackOffice.Boot;
internal class LogViewerMigration : MigrationBase
{
    private static string _uSyncLogQuery = "StartsWith(SourceContext, 'uSync')";

    private readonly ILogViewerConfig _logViewerConfig;

    public LogViewerMigration(
        ILogViewerConfig config,
        IMigrationContext context) : base(context)
    {
        _logViewerConfig = config;
    }

    protected override void Migrate()
    {
        var existing = _logViewerConfig.GetSavedSearches()
            .FirstOrDefault(x => x.Query.StartsWith(_uSyncLogQuery, StringComparison.OrdinalIgnoreCase));

        if (existing != null) return;
        
        _logViewerConfig.AddSavedSearch("Find all uSync Log Entries", _uSyncLogQuery);
        
    }
}
