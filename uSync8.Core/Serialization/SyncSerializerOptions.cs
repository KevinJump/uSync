using System.Collections.Generic;
using System.Configuration;
using NPoco.Expressions;
using Superpower.Model;
using Umbraco.Core;

namespace uSync8.Core.Serialization
{
    /// <summary>
    ///  options class that can be passed to a serialize/deserialize method.
    /// </summary>
    public class SyncSerializerOptions
    {
        public SyncSerializerOptions() { }

        public SyncSerializerOptions(SerializerFlags flags)
        {
            this.Flags = flags;
        }

        public SyncSerializerOptions(Dictionary<string, string> Settings)
        {
            this.Settings = Settings;
        }

        public SyncSerializerOptions(SerializerFlags flags, Dictionary<string, string> Settings)
        {
            this.Flags = flags;
            this.Settings = Settings;
        }

        /// <summary>
        ///  Only create item if it doesn't already exist.
        /// </summary>
        public bool CreateOnly { get; set; }

        /// <summary>
        ///  Serializer flags, turn things like DontSave, FailWhenParent is missing on
        /// </summary>
        /// <remarks>
        ///  this is now private - we might phase it out for the options instead.
        /// </remarks>
        public SerializerFlags Flags { get; internal set; }

        /// <summary>
        ///  Parameterized options, custom for each handler
        /// </summary>
        public Dictionary<string, string> Settings { get; internal set; }

        /// <summary>
        ///  flag properties, we can move this away from flags if we want to.
        /// </summary>
        public bool Force => Flags.HasFlag(SerializerFlags.Force);

        public bool DoNotSave => Flags.HasFlag(SerializerFlags.DoNotSave);

        public bool FailOnMissingParent => Flags.HasFlag(SerializerFlags.FailMissingParent);

        public bool OnePass => Flags.HasFlag(SerializerFlags.OnePass);


        public TResult GetSetting<TResult>(string key, TResult defaultValue)
        {
            if (this.Settings != null && this.Settings.ContainsKey(key))
            {
                var attempt = this.Settings[key].TryConvertTo<TResult>();
                if (attempt.Success)
                    return attempt.Result;
            }

            return defaultValue;
        }
    }
}
