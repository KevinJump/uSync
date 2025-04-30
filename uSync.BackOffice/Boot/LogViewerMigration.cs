using System.Threading.Tasks;

using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.BackOffice.Boot;
internal class LogViewerMigration : AsyncMigrationBase
{
    private static string _uSyncLogQuery = "StartsWith(SourceContext, 'uSync')";

    private readonly ILogViewerService _logViewerService;

    public LogViewerMigration(
        ILogViewerService logViewerService,
        IMigrationContext context) : base(context)
    {
        _logViewerService = logViewerService;
    }

    protected override async Task MigrateAsync()
    {
        var name = "Find all uSync Log Entries";

        var existing = await _logViewerService.GetSavedLogQueryByNameAsync(name);
        if (existing != null) return;

        await _logViewerService.AddSavedLogQueryAsync(name, _uSyncLogQuery);

    }
}
