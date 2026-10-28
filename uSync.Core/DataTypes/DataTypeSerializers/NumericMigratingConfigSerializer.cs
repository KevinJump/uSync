using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

/// <summary>
///  v19 moves min/max on the numeric editors to a single validation range.
/// </summary>
internal class NumericMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(NumericMigratingConfigSerializer);
    public string[] Editors => [
        Constants.PropertyEditors.Aliases.Integer,
        Constants.PropertyEditors.Aliases.Decimal];

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration.MigrateMinMaxToRange("min", "max", "validationRange");
}
