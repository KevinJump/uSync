using System;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class ContentPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        private readonly ILogger<ContentPickerConfigSerializer> _logger;

        public ContentPickerConfigSerializer(
            IEntityService entityService,
            ILogger<ContentPickerConfigSerializer> logger)
            : base(entityService)
        {
            _logger = logger;
        }

        public string Name => "ContentPickerNodeSerializer";

        public string[] Editors => ["Umbraco.ContentPicker"];

        public override string SerializeConfig(object configuration)
        {
            if (configuration is not ContentPickerConfiguration pickerConfig)
            {
                _logger.LogWarning("ContentPickerConfigSerializer called for non ContentPickerConfiguration type: {configType}", configuration.GetType());
                return base.SerializeConfig(configuration);
            }

            var contentPickerConfig = new MappedPathConfigBase<ContentPickerConfiguration>()
            {
                Config = new ContentPickerConfiguration()
                {
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    StartNodeId = null,
                    ShowOpenButton = pickerConfig.ShowOpenButton
                }
            };

            if (pickerConfig.StartNodeId != null)
                contentPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);

            return base.SerializeConfig(contentPickerConfig);
        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType != typeof(ContentPickerConfiguration))
            {
                _logger.LogWarning("ContentPickerConfigSerializer called for non ContentPickerConfiguration type: {configType}", configType);
                return base.DeserializeConfig(config, configType);
            }

            var mappedConfig = JsonConvert.DeserializeObject<MappedPathConfigBase<ContentPickerConfiguration>>(config);
            if (mappedConfig is null || mappedConfig.Config is null)
            {
                _logger.LogWarning("ContentPickerConfigSerializer failed to deserialize config: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (string.IsNullOrWhiteSpace(mappedConfig.MappedPath) is false)
            {
                mappedConfig.Config.StartNodeId = PathToUdi(mappedConfig.MappedPath);
            }
            else
            {
                _logger.LogDebug("ContentPickerConfigSerializer no MappedPath in config: {config}", config);
            }
            
            return mappedConfig.Config;
        }
    }
}
