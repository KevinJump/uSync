using System.Collections;
using System.Collections.Generic;
using System.Management;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;
using Umbraco.Core.Migrations.Expressions.Create;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Dependency
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

        /// <summary>
        ///  UDI reference for the object
        /// </summary>
        public Udi Udi { get; set; }

        /// <summary>
        ///  Order value, used to sort the imports, lower order items are imported first.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        ///  level in a tree - determains order within a section (lower level items imported first)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        ///  what mode is the dependency running in (MustExist or MustMatch)
        /// </summary>
        public DependencyMode Mode { get; set; }

        /// <summary>
        ///  Flags that control how an item is imported and sub dependencies calculated
        /// </summary>
        public DependencyFlags Flags { get; set; }

        /// <summary>
        ///  generic options that can be filled based on the type of item we have.
        /// </summary>
        public IDictionary<string, object> Options { get; set; }

        /// <summary>
        ///  Delegate event that you can listen to , to get messages about dependency updates.
        /// </summary>
        public static event uSyncDependencyUpdate DependencyUpdate;

        /// <summary>
        ///  fires an update message to anything listening to the DependencyUpdate event
        /// </summary>
        /// <param name="message"></param>
        public static void FireUpdate(string message)
        {
            FireUpdate(message, 1, 2);
        }


        /// <summary>
        ///  fires an update with progress to anything listing to the DependencyUpdate event
        /// </summary>
        public static void FireUpdate(string message, int count, int total)
        {
            DependencyUpdate?.Invoke(new DependencyMessageArgs
            {
                Message = message,
                Count = count,
                Total = total
            });
        }

        /// <summary>
        ///  Get a generic option out of the DependencyItem
        /// </summary>
        public T GetOption<T>(string key, T defaultValue)
        {
            if (this.Options != null && this.Options.ContainsKey(key))
            {
                var attempt = Options[key].TryConvertTo<T>();
                if (attempt.Success)
                    return attempt.Result;
            }

            return defaultValue;
        }

        /// <summary>
        ///  Add an option to the options for this item.
        /// </summary>
        public void SetOption<T>(string key, T value)
        {
            if (this.Options == null) this.Options = new Dictionary<string, object>();
            Options[key] = value;
        }

    }

    public enum DependencyMode
    {
        MustMatch,
        MustExist
    }

}
