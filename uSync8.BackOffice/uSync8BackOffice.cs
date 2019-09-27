using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice
{
    public class uSync8BackOffice
    {
        public static bool eventsPaused { get; set; }

        public static bool inStartup { get; set; }
    }

    internal class uSync
    {
        internal const string Name = "uSync8";
        internal class Trees
        {
            internal const string uSync = "uSync8";
            internal const string Group = "sync";
        }

        internal class Handlers
        {
            internal const string DefaultSet = "default";
        }
    }
}
