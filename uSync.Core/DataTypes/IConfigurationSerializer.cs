using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes;

public interface IConfigurationSerializer
{
    string Name { get; }
    string[] Editors { get; }

    [Obsolete("These are getting removed at release")]
    object? DeserializeConfig(string config, Type configType);

    [Obsolete("These are getting removed at release")]
    string? SerializeConfig(object configuration);

    IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
        => configuration;
    
    IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration;
}
