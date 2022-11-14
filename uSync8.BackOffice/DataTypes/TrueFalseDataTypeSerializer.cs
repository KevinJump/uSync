using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core.Logging;
using Umbraco.Web.PropertyEditors;

using uSync8.Core.DataTypes;

namespace uSync8.BackOffice.DataTypes
{
    internal class TrueFalseDataTypeSerializer : ConfigurationSerializerBase, IConfigurationSerializer
    {
        public string Name => "True/False DataType Serializer";
        public string[] Editors => new[] { "Umbraco.TrueFalse" };

        public override object DeserializeConfig(string config, Type configType)
        {
            try
            {
                // will attempt to do it in what ever version we are currenlty running
                return JsonConvert.DeserializeObject<TrueFalseConfiguration>(config);
            }
            catch
            {
                // this happens if a pre v8.8 true/false has a default value of 'null'
                // then we use reflection to serialize into the new format. 
                var oldConfig = JsonConvert.DeserializeObject<OldTrueFalseConfiguration>(config);

                bool.TryParse(oldConfig.Default, out bool defaultValue);

                var newconfig = Activator.CreateInstance(configType);

                var defaultProperty = configType.GetProperty("Default", typeof(bool));
                if (defaultProperty != null)
                {
                    defaultProperty.SetValue(newconfig, defaultValue);
                }

                var labelOnProperty = configType.GetProperty("LabelOn", typeof(string));
                if (labelOnProperty != null)
                {
                    labelOnProperty.SetValue(newconfig, oldConfig.Label);
                }

                var labelOffProperty = configType.GetProperty("LabelOff", typeof(string));
                if (labelOffProperty != null)
                {
                    labelOffProperty.SetValue(newconfig, oldConfig.Label);
                }

                if (!string.IsNullOrEmpty(oldConfig.Label))
                {
                    var showLabelsProperty = configType.GetProperty("ShowLabels", typeof(bool));
                    if (showLabelsProperty != null)
                    {
                        showLabelsProperty.SetValue(newconfig, true);
                    }
                }

                return newconfig;
            }
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class OldTrueFalseConfiguration
    {
        public string Default { get; set; }
        public string Label { get; set; }
    }

}
