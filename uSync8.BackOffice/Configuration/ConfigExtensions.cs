using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Configuration;

namespace uSync8.BackOffice.Configuration
{
    public static class ConfigExtensions
    {
        public static uSyncSettings uSync(this Configs configs)
            => configs.GetConfig<uSyncConfig>().Settings;
    }
}
