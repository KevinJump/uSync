using System;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MediaPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        private readonly ILogger<MediaPickerConfigSerializer> _logger;

        public MediaPickerConfigSerializer(
            IEntityService entityService,
            ILogger<MediaPickerConfigSerializer> logger)
            : base(entityService)
        {
            _logger = logger;
        }

        public string Name => "MediaPickerNodeSerializer";

        public string[] Editors => ["Umbraco.MediaPicker"];

        public override string SerializeConfig(object configuration)
        {
            if (configuration is not MediaPickerConfiguration pickerConfig)
            {
                _logger.LogWarning("MediaPickerConfigSerializer called for non MediaPickerConfiguration type: {configType}", configuration.GetType());
                return base.SerializeConfig(configuration);
            }

            var mediaPickerConfig = new MappedPathConfigBase<MediaPickerConfiguration>() {
                Config = new MediaPickerConfiguration()
                {
                    DisableFolderSelect = pickerConfig.DisableFolderSelect,
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    Multiple = pickerConfig.Multiple,
                    OnlyImages = pickerConfig.OnlyImages,
                    StartNodeId = null
                }
            };

            if (pickerConfig.StartNodeId != null)
                mediaPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);

            return base.SerializeConfig(mediaPickerConfig);
        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType != typeof(MediaPickerConfiguration))
            {
                _logger.LogWarning("MediaPickerConfigSerializer called for non MediaPickerConfiguration type: {configType}", configType);
                return base.DeserializeConfig(config, configType);
            }

            var mappedConfig = JsonConvert.DeserializeObject<MappedPathConfigBase<MediaPickerConfiguration>>(config);
            if (mappedConfig is null)
            {
                _logger.LogWarning("MediaPickerConfigSerializer failed to deserialize config: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (!string.IsNullOrWhiteSpace(mappedConfig.MappedPath))
            {
                mappedConfig.Config.StartNodeId = PathToUdi(mappedConfig.MappedPath);
            }

            return mappedConfig.Config;

        }
    }
}
