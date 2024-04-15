using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class MarkdownMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(MarkdownMigratingConfigSerializer);

	public string[] Editors => [ Constants.PropertyEditors.Aliases.MarkdownEditor ];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
		=> MigratePropertyNames(configuration, new()
			{
				{ "displayLivePreview", "preview" }
			});
}
