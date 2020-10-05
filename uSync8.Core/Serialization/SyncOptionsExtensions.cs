using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Serialization
{
    public static class SyncOptionsExtensions
    {
        /// <summary>
        ///  perform the removal of properties and items. 
        /// </summary>
        public static bool DeleteItems(this SyncSerializerOptions options)
            => !options.GetSetting<bool>(uSyncConstants.DefaultSettings.NoRemove, uSyncConstants.DefaultSettings.NoRemove_Default);
    }
}
