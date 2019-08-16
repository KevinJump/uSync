using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.SyncHandlers
{
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

        [SyncActionName("save")]
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
