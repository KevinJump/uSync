using System;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MediaPicker3ConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        private readonly ILogger<MediaPicker3ConfigSerializer> _logger;

        public MediaPicker3ConfigSerializer(
            IEntityService entityService,
            ILogger<MediaPicker3ConfigSerializer> logger)
            : base(entityService)
        {
            _logger = logger;
        }

        public string Name => "MediaPicker3NodeSerializer";

        public string[] Editors => ["Umbraco.MediaPicker3"];

        public override string SerializeConfig(object configuration)
        {
            if (configuration is not MediaPicker3Configuration pickerConfig)
            {
                _logger.LogWarning("MediaPicker3ConfigSerializer called for non MediaPicker3Configuration type: {configType}", configuration.GetType());
                return base.SerializeConfig(configuration);
            }

            var mediaPickerConfig = new MappedPathConfigBase<MediaPicker3Configuration>()
            {
                Config = new MediaPicker3Configuration()
                {
                    EnableLocalFocalPoint = pickerConfig.EnableLocalFocalPoint,
                    Crops = pickerConfig.Crops,
                    Filter = pickerConfig.Filter,
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    Multiple = pickerConfig.Multiple,
                    ValidationLimit = pickerConfig.ValidationLimit
                }
            };

            if (pickerConfig.StartNodeId != null)
                mediaPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);

            return base.SerializeConfig(mediaPickerConfig);
        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType != typeof(MediaPicker3Configuration))
            {
                _logger.LogWarning("MediaPicker3ConfigSerializer called for non MediaPicker3Configuration type: {configType}", configType);
                return base.DeserializeConfig(config, configType);
            }

            var mappedConfig = JsonConvert.DeserializeObject<MappedPathConfigBase<MediaPicker3Configuration>>(config);
            if (mappedConfig is null)
            {
                _logger.LogWarning("MediaPicker3ConfigSerializer failed to deserialize config: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (string.IsNullOrWhiteSpace(mappedConfig.MappedPath) is false)
                mappedConfig.Config.StartNodeId = PathToUdi(mappedConfig.MappedPath);

            return mappedConfig.Config;
        }
    }
}
