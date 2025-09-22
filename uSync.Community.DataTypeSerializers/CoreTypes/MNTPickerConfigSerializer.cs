using System;

using Newtonsoft.Json;

using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.PropertyEditors;

using uSync.Core.DataTypes;
using Microsoft.Extensions.Logging;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MNTPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        private readonly ILogger<MediaPicker3ConfigSerializer> _logger;
        public MNTPickerConfigSerializer(
            IEntityService entityService,
            ILogger<MediaPicker3ConfigSerializer> logger)
            : base(entityService)
        {
            _logger = logger;
        }

        public string Name => "MNTPNodeSerializer";

        public string[] Editors => ["Umbraco.MultiNodeTreePicker"];

        public override string SerializeConfig(object configuration)
        {
            if (configuration is not MultiNodePickerConfiguration pickerConfig)
            {
                _logger.LogWarning("MNTPickerConfigSerializer called for non MultiNodePickerConfiguration type: {configType}", configuration.GetType());
                return base.SerializeConfig(configuration);
            }

            var MNTPMappedConfig = new MappedPathConfigBase<MultiNodePickerConfiguration>()
            {

                Config = new MultiNodePickerConfiguration()
                {
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    Filter = pickerConfig.Filter,
                    MaxNumber = pickerConfig.MaxNumber,
                    MinNumber = pickerConfig.MinNumber,
                    ShowOpen = pickerConfig.ShowOpen,
                    TreeSource = new MultiNodePickerConfigurationTreeSource()
                    {
                        ObjectType = pickerConfig.TreeSource?.ObjectType,
                        StartNodeId = pickerConfig.TreeSource?.StartNodeId,
                        StartNodeQuery = pickerConfig.TreeSource?.StartNodeQuery
                    }
                }
            };

            if (pickerConfig?.TreeSource?.StartNodeId != null)
            {
                MNTPMappedConfig.MappedPath = UdiToEntityPath(pickerConfig.TreeSource.StartNodeId);
            }

            return base.SerializeConfig(MNTPMappedConfig);
        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType != typeof(MultiNodePickerConfiguration))
            {
                _logger.LogWarning("MNTPickerConfigSerializer called for non MultiNodePickerConfiguration type: {configType}", configType);
                return base.DeserializeConfig(config, configType);
            }

            var mappedConfig = JsonConvert.DeserializeObject<MappedPathConfigBase<MultiNodePickerConfiguration>>(config);
            if (mappedConfig is null) {
                _logger.LogWarning("MNTPickerConfigSerializer failed to deserialize config: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (mappedConfig.Config.TreeSource == null) {
                _logger.LogWarning("MNTPickerConfigSerializer deserialized config has no TreeSource section: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (!string.IsNullOrWhiteSpace(mappedConfig.MappedPath) && mappedConfig.Config.TreeSource != null)
            {
                mappedConfig.Config.TreeSource.StartNodeId = PathToUdi(mappedConfig.MappedPath);
            }

            return mappedConfig.Config;
        }
    }
}
