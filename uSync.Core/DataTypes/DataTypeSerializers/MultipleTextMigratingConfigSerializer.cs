using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class MultipleTextMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(MultipleTextMigratingConfigSerializer);

	public string[] Editors => [Constants.PropertyEditors.Aliases.MultipleTextstring];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
		=> MigratePropertyNames(configuration, new()
		{
			{ "maximum", "max" },
			{ "minimum", "min" }
		});
}
