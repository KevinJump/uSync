using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uSync.Core.Dependency;

namespace uSync.Core.DataTypes
{
    public interface IConfigurationSerializer
    {
        string Name { get; }
        string[] Editors { get; }

        object DeserializeConfig(string config, Type configType);

        string SerializeConfig(object configuration);
    }
}
