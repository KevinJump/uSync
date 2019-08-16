using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.SyncHandlers
{
    /*
    public static class HandlerActionNames
    {
        public const string Report = "report";
        public const string Import = "import";
        public const string Export = "export";
        public const string Save = "save";
        public const string All = "all";
    }
    */


    public enum HandlerActions
    {
        [SyncActionName("")]
        None,

        [SyncActionName("report")]
        Report,

        [SyncActionName("import")]
        Import,

        [SyncActionName("export")]
        Export,

        [SyncActionName("Save")]
        Save,

        [SyncActionName("All")]
        All
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    internal class SyncActionName : Attribute
    {
        private readonly string name;

        public SyncActionName(string name)
        {
            this.name = name;
        }

        public override string ToString()
            => this.name;
    }
}
