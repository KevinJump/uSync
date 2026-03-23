using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.Core.Migrations.Migrations;

internal class CreateMigratedDataTable : AsyncMigrationBase
{
    public CreateMigratedDataTable(IMigrationContext context) : base(context)
    { }

    protected override Task MigrateAsync()
    {
        if (!TableExists(SyncMigrations.MigratedDataTableName))
            Create.Table<SyncMigratedData>().Do();

        return Task.CompletedTask;
    }
}
