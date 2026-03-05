using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.Core.Migrations.Migrations;

internal class SyncMigratedDataMigrationPlan : MigrationPlan
{
    public SyncMigratedDataMigrationPlan() 
        : base(SyncMigrations.AppName)
    {
        From(string.Empty)
            .To<CreateMigratedDataTable>("Add SyncMigratedData Table");
    }
}
