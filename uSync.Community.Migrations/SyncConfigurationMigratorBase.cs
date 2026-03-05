using uSync.Core.DataTypes;

namespace uSync.Community.Migrations;

/// <summary>
///  Migrating serializer for migrating the datatype configuration
/// </summary>
/// <remarks>
///  This is really just a wrapper over the standard <seealso cref="ConfigurationSerializerBase"/> 
///  to make it easier to implement migrating serializers, and to make it more explicit that the
///  serializer is for migrating configuration.
/// </remarks>
public abstract class SyncConfigurationMigratorBase : ConfigurationSerializerBase
{
    /// <summary>
    ///  the editor that the datatype will become once it has been migrated. 
    /// </summary>
    public abstract string? TargetEditor { get; }

    /// <summary>
    ///  Method to migrate the configuration, this will be called during import, and should return the migrated configuration.
    /// </summary>
    /// <remarks>
    ///  it is safe to assume that the migration has not already happened here, because once a migration is 
    ///  completed the property will be of the new type so this should not be called again. 
    /// </remarks>
    /// <param name="configuration">The current configuration of the datatype.</param>
    /// <returns>The migrated configuration.</returns>
    public abstract IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration);



    public string? GetEditorAlias() => TargetEditor;
    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => GetMigratedConfiguration(configuration);

}
