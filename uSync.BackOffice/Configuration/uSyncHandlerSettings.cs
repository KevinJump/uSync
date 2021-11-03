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
        /// <summary>
        ///  is handler enabled or disabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///  list of actions the handler is configured for. 
        /// </summary>
        public string[] Actions { get; set; } = Array.Empty<string>();

        /// <summary>
        ///  should use a flat folder structure when exporting items
        /// </summary>
        public bool UseFlatStructure { get; set; } = true;

        /// <summary>
        ///  items should be saved with their guid/key value as the filename
        /// </summary>
        public bool GuidNames { get; set; } = false;

        /// <summary>
        ///  imports should fail if the parent item is missing (if false, item be importated go a close as possible to location)
        /// </summary>
        public bool FailOnMissingParent { get; set; } = false;

        /// <summary>
        ///  override the group the handler belongs too.
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        ///  additional settings for the handler
        /// </summary>
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

        /// <summary>
        ///  create a copy of this handlers settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
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
