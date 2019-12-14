using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync8.Core.Dependency
{
    public delegate void uSyncDependencyUpdate(DependencyMessageArgs e);

    public class DependencyMessageArgs
    {
        public string Message { get; set; }
    }


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

        public static event uSyncDependencyUpdate DependencyUpdate;

        public static void FireUpdate(string message)
        {
            DependencyUpdate?.Invoke(new DependencyMessageArgs
            {
                Message = message
            });
        }
    }

    public enum DependencyMode
    {
        MustMatch,
        MustExist
    }

}
