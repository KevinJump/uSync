﻿using System.Collections.Generic;

namespace uSync8.BackOffice.Configuration
{
    public class HandlerSettings
    {
        public string Alias { get; }
        public bool Enabled { get; set; }

        public string[] Actions { get; set; } = new string[] { "All" };

        public OverriddenValue<bool> UseFlatStructure { get; set; } = new OverriddenValue<bool>();
        public OverriddenValue<bool> GuidNames { get; set; } = new OverriddenValue<bool>();

        public OverriddenValue<bool> BatchSave { get; set; } = new OverriddenValue<bool>();

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public HandlerSettings(string alias, bool enabled)
        {
            Alias = alias;
            Enabled = enabled;
        }
    }

}
