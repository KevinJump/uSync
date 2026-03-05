using Umbraco.Cms.Core.PropertyEditors;

using uSync.Core.DataTypes;

namespace uSync.Community.Migrations;

/// <summary>
///  Serves as an abstract base class for migrating configuration settings that depend on specific property editors.
/// </summary>
/// <remarks>Derived classes must specify the target property editor by implementing the TargetEditor property.
/// This class provides a framework for transforming configuration dictionaries to support migration scenarios where
/// configuration is editor-dependent. Use GetMigratedConfiguration to obtain the updated configuration for a given
/// editor.</remarks>
public abstract class SyncDependenantConfigurationMigratorBase : ConfigurationDependenantSerializerBase
{
    protected SyncDependenantConfigurationMigratorBase(PropertyEditorCollection propertyEditors)
        : base(propertyEditors)
    { }

    public abstract string? TargetEditor { get; }
    public string? GetEditorAlias() => TargetEditor;
    public abstract IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration);
    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => GetMigratedConfiguration(configuration);
}
