using System.Configuration;
using System.Security.Cryptography;

using Umbraco.Core;

namespace uSync8.Core
{
    public static class uSyncHashAlgorithm
    {
        static string configFips = ConfigurationManager.AppSettings["uSync.ForceFips"];

        public static HashAlgorithm Create()
            => HashAlgorithm.Create(FipsOnly ? "SHA1" : "MD5");

        public static bool FipsOnly =>
            (CryptoConfig.AllowOnlyFipsAlgorithms || (configFips != null && configFips.InvariantEquals("true")));
    }
}
