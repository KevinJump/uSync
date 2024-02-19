using System;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MediaPicker3ConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        public MediaPicker3ConfigSerializer(IEntityService entityService)
            : base(entityService)
        { }

        public string Name => "MediaPicker3NodeSerializer";

        public string[] Editors => new string[] { "Umbraco.MediaPicker3" };

        public override string SerializeConfig(object configuration)
        {

            if (configuration is MediaPicker3Configuration pickerConfig)
            {
                var mediaPickerConfig = new MappedPathConfigBase<MediaPicker3Configuration>();
                mediaPickerConfig.Config = new MediaPicker3Configuration()
                {
                    EnableLocalFocalPoint = pickerConfig.EnableLocalFocalPoint,
                    Crops = pickerConfig.Crops,
                    Filter = pickerConfig.Filter,
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    Multiple = pickerConfig.Multiple,
                    ValidationLimit = pickerConfig.ValidationLimit
                };

                if (pickerConfig.StartNodeId != null)
                    mediaPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);
                return base.SerializeConfig(mediaPickerConfig);
            }

            return base.SerializeConfig(configuration);

        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType == typeof(MediaPicker3Configuration))
            {
                var mappedConfig = config.Deserialize<MappedPathConfigBase<MediaPicker3Configuration>>();

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
