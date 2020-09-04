using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class HandlerSettings
    {
        public string Alias { get; }
        public bool Enabled { get; set; }

        public string[] Actions { get; set; } = new string[] { "All" };

        public OverriddenValue<bool> UseFlatStructure { get; set; } = new OverriddenValue<bool>();
        public OverriddenValue<bool> GuidNames { get; set; } = new OverriddenValue<bool>();

        public OverriddenValue<bool> FailOnMissingParent { get; set; } = new OverriddenValue<bool>();

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public HandlerSettings(string alias, bool enabled)
        {
            Alias = alias;
            Enabled = enabled;
        }

        /// <summary>
        ///  Get a specific setting from the Settings collection for this handler.
        /// </summary>
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
