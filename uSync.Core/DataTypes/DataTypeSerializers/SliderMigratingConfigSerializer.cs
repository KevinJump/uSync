using NPoco.Linq;

using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class SliderMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(SliderMigratingConfigSerializer);
	public string[] Editors => [Constants.PropertyEditors.Aliases.Slider];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
		=> MigratePropertyNames(configuration, new()
		{
			{ "initialValue", "initVal1" },
			{ "initialValue2", "initVal2" },
			{ "maximumValue", "maxVal" },
			{ "minimumValue", "minVal" },
			{ "stepIncrements", "step" }
		});
}
