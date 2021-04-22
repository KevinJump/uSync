using System;

using Newtonsoft.Json;

namespace uSync.Core.DataTypes
{
    public abstract class ConfigurationSerializerBase
    {
        public virtual object DeserializeConfig(string config, Type configType)
        {
            return JsonConvert.DeserializeObject(config, configType);
        }

        public virtual string SerializeConfig(object configuration)
        {
            return JsonConvert.SerializeObject(configuration, Formatting.Indented);
        }

    }
}
