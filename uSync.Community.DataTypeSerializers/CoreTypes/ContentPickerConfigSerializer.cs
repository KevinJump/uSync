using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync8.Community.DataTypeSerializers.CoreTypes
{
    public class ContentPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
    {
        public ContentPickerConfigSerializer(IEntityService entityService)
            : base(entityService)
        { }

        public string Name => "ContentPickerNodeSerializer";

        public string[] Editors => new string[] { "Umbraco.ContentPicker" };

        public override IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
        {
            return base.GetConfigurationExport(configuration);
        }

        public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        {
            return base.GetConfigurationImport(configuration);
        }

        //public override string? SerializeConfig(object configuration)
        //{

        //    if (configuration is ContentPickerConfiguration pickerConfig)
        //    {
        //        var contentPickerConfig = new MappedPathConfigBase<ContentPickerConfiguration>();

        //        contentPickerConfig.Config = new ContentPickerConfiguration()
        //        {
        //            IgnoreUserStartNodes = pickerConfig.IgnoreUserStartNodes,
        //            //StartNodeId = null,
        //            //ShowOpenButton = pickerConfig.ShowOpenButton
        //        };

        //        //if (pickerConfig.StartNodeId != null)
        //        //    contentPickerConfig.MappedPath = UdiToEntityPath(pickerConfig.StartNodeId);

        //        return base.SerializeConfig(contentPickerConfig);
        //    }

        //    return base.SerializeConfig(configuration);
        //}


        //public override object? DeserializeConfig(string config, Type configType)
        //{
        //    if (configType == typeof(ContentPickerConfiguration))
        //    {
        //        var mappedConfig =   config.DeserializeJson<MappedPathConfigBase<ContentPickerConfiguration>>();

        //        //if (!string.IsNullOrWhiteSpace(mappedConfig.MappedPath))
        //        //{
        //        //    mappedConfig.Config.StartNodeId = PathToUdi(mappedConfig.MappedPath);
        //        //}

        //        return mappedConfig?.Config;
        //    }

        //    return base.DeserializeConfig(config, configType);
        //}
    }
}
