using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace uSync.Core
{
    public static class SyncPropertyGroupHelpers
    {
        /// <summary>
        ///  prefix we put on tabs when we are appending something to them.
        /// </summary>
        public static string uSyncTmpTabAliasPrefix = "zzzusync";

        /// <summary>
        ///  Get the temp alias for the tab - that will help with clashes on renames/changes of type
        /// </summary>
        public static string GetTempTabAlias(string alias)
            => $"{uSyncTmpTabAliasPrefix}{alias}";

        /// <summary>
        ///  is the current tab alias a temp alias (one we have appended zzz... to)
        /// </summary>
        public static bool IsTempTabAlias(string alias)
            => !string.IsNullOrWhiteSpace(alias) && alias.StartsWith(uSyncTmpTabAliasPrefix);

        /// <summary>
        ///  strip of any temp tab alias string from the tab name.
        /// </summary>
        public static string StripTempTabAlias(string alias)
        {
            if (IsTempTabAlias(alias))
                return alias.Substring(uSyncTmpTabAliasPrefix.Length);

            return alias;
        }

        public static PropertyGroup FindTab(this PropertyGroupCollection groups, string alias)
        {
            var tab = groups.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
            if (tab != null) return tab;

            var tempAlias = GetTempTabAlias(alias);
            tab = groups.FirstOrDefault(x => x.Alias.InvariantEquals(tempAlias));
            if (tab != null) return tab;

            return null;
        }
    }
}
