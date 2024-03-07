using System;
using System.Linq;

using Umbraco.Cms.Core.Logging.Viewer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.BackOffice.Boot;
internal class LogViewerMigration : MigrationBase
{
    private static string uSyncLogQuery = "StartsWith(SourceContext, 'uSync')";

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
            .FirstOrDefault(x => x.Query.StartsWith(uSyncLogQuery, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            _logViewerConfig.AddSavedSearch("Find all uSync Log Entries", uSyncLogQuery);
        }
    }
}
