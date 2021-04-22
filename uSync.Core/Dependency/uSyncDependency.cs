using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Cms.Core;

namespace uSync.Core.Dependency
{
    public delegate void uSyncDependencyUpdate(DependencyMessageArgs e);

    public class DependencyMessageArgs
    {
        public string Message { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
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
            FireUpdate(message, 1, 2);
        }

        public static void FireUpdate(string message, int count, int total)
        {
            DependencyUpdate?.Invoke(new DependencyMessageArgs
            {
                Message = message,
                Count = count,
                Total = total
            });
        }
    }

    public enum DependencyMode
    {
        MustMatch,
        MustExist
    }

}
