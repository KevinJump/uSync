using System;

namespace uSync8.Core.DataTypes
{
    public interface IConfigurationSerializer
    {
        string Name { get; }
        string[] Editors { get; }

        object DeserializeConfig(string config, Type configType);

        string SerializeConfig(object configuration);
    }
}
