using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Extensions;

namespace uSync.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class HandlerSettings
    {
        public bool Enabled { get; set; } = true;

        public string[] Actions { get; set; } = Array.Empty<string>();

        public bool UseFlatStructure { get; set; } = true;
        public bool GuidNames { get; set; } = false;
        public bool FailOnMissingParent { get; set; } = false;

        public string Group { get; set; } = string.Empty;

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public HandlerSettings() { }
    }

    public static class HandlerSettingsExtensions
    {
        /// <summary>
        ///  get a setting from the settings dictionary.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="settings"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TResult GetSetting<TResult>(this HandlerSettings settings, string key, TResult defaultValue)
        {
            if (settings.Settings != null && settings.Settings.ContainsKey(key))
            {
                var attempt = settings.Settings[key].TryConvertTo<TResult>();
                if (attempt) return attempt.Result;
            }

            return defaultValue;
        }

        /// <summary>
        ///  Add a setting to the settings Dictionary (creating the dictionary if its missing)
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddSetting<TObject>(this HandlerSettings settings, string key, TObject value)
        {
            if (settings.Settings == null)
                settings.Settings = new Dictionary<string, string>();

            settings.Settings[key] = value.ToString();
        }

        public static HandlerSettings Clone(this HandlerSettings settings)
        {
            return new HandlerSettings
            {
                Actions = settings.Actions,
                Enabled = settings.Enabled,
                FailOnMissingParent = settings.FailOnMissingParent,
                UseFlatStructure = settings.UseFlatStructure,
                Group = settings.Group,
                GuidNames = settings.GuidNames,
                Settings = new Dictionary<string, string>(settings.Settings)
            };
        }

    }

}
