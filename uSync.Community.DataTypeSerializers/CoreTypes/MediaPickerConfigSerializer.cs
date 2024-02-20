using System;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MediaPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        public MediaPickerConfigSerializer(IEntityService entityService)
            : base(entityService)
        { }

        public string Name => "MediaPickerNodeSerializer";

        public string[] Editors => new string[] { "Umbraco.MediaPicker" };

        public override string SerializeConfig(object configuration)
        {

            if (configuration is MediaPickerConfiguration pickerConfig)
            {
                var mediaPickerConfig = new MappedPathConfigBase<MediaPickerConfiguration>();
                mediaPickerConfig.Config = new MediaPickerConfiguration()
                {
                    DisableFolderSelect = pickerConfig.DisableFolderSelect,
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    Multiple = pickerConfig.Multiple,
                    OnlyImages = pickerConfig.OnlyImages,
                    StartNodeId = null
                };

                if (pickerConfig.StartNodeId != null)
                    mediaPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);
                return base.SerializeConfig(mediaPickerConfig);
            }

            return base.SerializeConfig(configuration);

        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType == typeof(MediaPickerConfiguration))
            {
                var mappedConfig = config.DeserializeJson<MappedPathConfigBase<MediaPickerConfiguration>>();

                if (!string.IsNullOrWhiteSpace(mappedConfig.MappedPath))
                {
                    mappedConfig.Config.StartNodeId = PathToUdi(mappedConfig.MappedPath);
                }

                return mappedConfig.Config;
            }

            return base.DeserializeConfig(config, configType);
        }
    }
}
