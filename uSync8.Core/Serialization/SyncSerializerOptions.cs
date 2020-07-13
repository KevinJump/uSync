using System.Collections.Generic;
using System.Configuration;
using System.Xml.Linq;

using NPoco.Expressions;
using Superpower.Model;
using Umbraco.Core;

using uSync8.Core.Extensions;

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

        public SyncSerializerOptions(Dictionary<string, string> settings)
        {
            this.Settings = settings ?? new Dictionary<string, string>();

        }

        public SyncSerializerOptions(SerializerFlags flags, Dictionary<string, string> settings)
            : this(settings)
        {
            this.Flags = flags;
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

        // public bool DoNotSave => Flags.HasFlag(SerializerFlags.DoNotSave);

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

        /// <summary>
        ///  Get the cultures defined in the settings.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetCultures() => GetSetting(uSyncConstants.CultureKey, string.Empty).ToDelimitedList();

        /// <summary>
        ///  Gets the cultures that can be de-serialized from this node.
        /// </summary>
        /// <remarks>
        ///  If a node has 'Cultures' set on the top node, then its only a partial sync
        ///  so we need to treat all the functions like this is set on the handler..
        /// </remarks>
        public IList<string> GetDeserializedCultures(XElement node)
        {
            var nodeCultures = node.GetCultures();
            if (!string.IsNullOrEmpty(nodeCultures)) return nodeCultures.ToDelimitedList();
            return GetCultures();
        }

        public IList<string> GetSegments()
            => GetSetting(uSyncConstants.SegmentKey, string.Empty).ToDelimitedList();

        public string SwapValue(string key, string newValue)
        {
            string oldValue = null;

            if (!this.Settings.ContainsKey(key))
                oldValue = this.Settings[key];

            if (newValue == null)
                this.Settings.Remove(key);
            else
                this.Settings[key] = newValue;

            return oldValue;
        }
    }
}
