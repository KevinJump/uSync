using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.BackOffice.Boot;
internal class LogViewerMigration : MigrationBase
{
    private static string _uSyncLogQuery = "StartsWith(SourceContext, 'uSync')";

    private readonly ILogViewerService _logViewerService;

    public LogViewerMigration(
		ILogViewerService logViewerService,
        IMigrationContext context) : base(context)
    {
        _logViewerService = logViewerService;
    }

    protected override void Migrate()
    {
        var name = "Find all uSync Log Entries";

        var existing = _logViewerService.GetSavedLogQueryByNameAsync(name).Result;
        if (existing != null) return;

        _logViewerService
            .AddSavedLogQueryAsync(name, _uSyncLogQuery).Wait();
        
    }
}
