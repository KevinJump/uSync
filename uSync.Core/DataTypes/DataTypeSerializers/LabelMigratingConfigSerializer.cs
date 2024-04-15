using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class LabelMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(LabelMigratingConfigSerializer);
	public string[] Editors => [Constants.PropertyEditors.Aliases.Label];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
		=> MigratePropertyNames(configuration, new()
		{
			{ "ValueType", "umbracoDataValueType" }
		});
}