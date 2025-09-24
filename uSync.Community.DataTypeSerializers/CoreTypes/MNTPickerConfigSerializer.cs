using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class MNTPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        private const string _keyOriginAlias = "ByKey";

        private readonly ILogger<MNTPickerConfigSerializer> _logger;
        public MNTPickerConfigSerializer(
            IEntityService entityService,
            ILogger<MNTPickerConfigSerializer> logger)
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
                        StartNodeQuery = pickerConfig.TreeSource?.StartNodeQuery,
                        DynamicRoot = pickerConfig.TreeSource?.DynamicRoot
                    }
                }
            };

            if (pickerConfig.TreeSource?.StartNodeId is not null)
            {
                MNTPMappedConfig.MappedPath = UdiToEntityPath(pickerConfig.TreeSource.StartNodeId);
            }

            if (pickerConfig.TreeSource?.DynamicRoot?.OriginAlias.Equals(_keyOriginAlias) is true
                && pickerConfig.TreeSource.DynamicRoot.OriginKey.HasValue is true)
            {
                MNTPMappedConfig.MappedRoot = GuidToEntityPath(pickerConfig.TreeSource.DynamicRoot.OriginKey.Value);
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
            if (mappedConfig is null)
            {
                _logger.LogWarning("MNTPickerConfigSerializer failed to deserialize config: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (mappedConfig.Config.TreeSource is null)
            {
                _logger.LogWarning("MNTPickerConfigSerializer deserialized config has no TreeSource section: {config}", config);
                return base.DeserializeConfig(config, configType);
            }

            if (string.IsNullOrWhiteSpace(mappedConfig.MappedPath) is false 
                && mappedConfig.Config.TreeSource is not null)
            {
                mappedConfig.Config.TreeSource.StartNodeId = PathToUdi(mappedConfig.MappedPath);
            }

            if (mappedConfig.Config.TreeSource?.DynamicRoot?.OriginAlias.Equals(_keyOriginAlias) is true
                && (string.IsNullOrWhiteSpace(mappedConfig.MappedRoot) is false)) 
            {
                mappedConfig.Config.TreeSource.DynamicRoot.OriginKey = PathToGuid(mappedConfig.MappedRoot);
            }

            return mappedConfig.Config;
        }
    }
}
