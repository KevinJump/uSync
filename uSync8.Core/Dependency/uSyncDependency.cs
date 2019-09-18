using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync8.Core.Dependency
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncDependency
    {
        /// <summary>
        ///  name to display to user (not critical for deployment of a dependency)
        /// </summary>
        public string Name { get; set; }

        public Udi Udi { get; set; }
        public int Order { get; set; }

        public int Level { get; set; }

        public DependencyMode Mode { get; set; }

        public DependencyFlags Flags { get; set; }
    }

    public enum DependencyMode
    {
        MustMatch,
        MustExist
    }
}
