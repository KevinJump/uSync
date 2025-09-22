using System;

namespace uSync.Core.DataTypes
{
    /// <summary>
    /// Interface for datatype configuration serializers
    /// </summary>
    public interface IConfigurationSerializer
    {
        /// <summary>
        ///  display name of the serializer
        /// </summary>
        string Name { get; }

        /// <summary>
        ///  editor aliases that this serializer can handle
        /// </summary>
        string[] Editors { get; }

        /// <summary>
        ///  deserialize the config string into the config type
        /// </summary>
        object DeserializeConfig(string config, Type configType);

        /// <summary>
        ///  serialize the config object into a config string
        /// </summary>
        string SerializeConfig(object configuration);
    }
}
