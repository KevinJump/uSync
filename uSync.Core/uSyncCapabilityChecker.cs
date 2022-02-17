using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Configuration;

namespace uSync.Core
{
    /// <summary>
    ///  a centralized way of telling if the current version of umbraco has 
    ///  certain features or not. 
    /// </summary>
    public class uSyncCapabilityChecker
    {
        private readonly IUmbracoVersion _version;
        public uSyncCapabilityChecker(IUmbracoVersion version)
        {
            _version = version;
        }

        /// <summary>
        ///  History cleanup was introduced in Umbraco 9.1 
        /// </summary>
        /// <remarks>
        ///  anything above v9.1 has history cleanup.
        /// </remarks>
        public bool HasHistoryCleanup
            => _version.Version.Major != 9 || _version.Version.Minor >= 1;
    }
}
