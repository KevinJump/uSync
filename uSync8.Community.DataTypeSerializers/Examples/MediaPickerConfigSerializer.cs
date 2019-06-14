using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSync8.Core.DataTypes;

namespace uSync8.Community.DataTypeSerializers.Examples
{
    public class MediaPickerConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
    {
        public string Name => "MediaPickerNoStartNodeSerilizer";

        public string[] Editors => new string[] { "Umbraco.MediaPicker" };

        public override string SerializeConfig(object configuration)
        {
            if (configuration is Umbraco.Web.PropertyEditors.MediaPickerConfiguration pickerConfig)
            {
                pickerConfig.StartNodeId = null;
            }

            return base.SerializeConfig(configuration);
        }
    }
}
