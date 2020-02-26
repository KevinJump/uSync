﻿using System;

using Newtonsoft.Json;

using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;

using uSync8.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class ContentPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        public ContentPickerConfigSerializer(IEntityService entityService)
            : base(entityService)
        { }

        public string Name => "ContentPickerNodeSerializer";

        public string[] Editors => new string[] { "Umbraco.ContentPicker" };

        public override string SerializeConfig(object configuration)
        {

            if (configuration is ContentPickerConfiguration pickerConfig)
            {
                var contentPickerConfig = new MappedPathConfigBase<ContentPickerConfiguration>();

                contentPickerConfig.Config = new ContentPickerConfiguration()
                {
                    IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
                    StartNodeId = null,
                    ShowOpenButton = pickerConfig.ShowOpenButton
                };

                if (pickerConfig.StartNodeId != null)
                    contentPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);

                return base.SerializeConfig(contentPickerConfig);
            }

            return base.SerializeConfig(configuration);
        }


        public override object DeserializeConfig(string config, Type configType)
        {
            if (configType == typeof(ContentPickerConfiguration))
            {
                var mappedConfig = JsonConvert.DeserializeObject<MappedPathConfigBase<ContentPickerConfiguration>>(config);

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
