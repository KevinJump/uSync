using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerAttribute : Attribute
    {
        public SyncHandlerAttribute(string alias, string name, string folder, int priority)
        {
            Alias = alias;
            Name = name;
            Priority = priority;
            Folder = folder;
        }

        public string Name { get; set; }
        public string Alias { get; set; }
        public int Priority { get; set; }
        public string Folder { get; set; }

        public bool IsTwoPass { get; set; } = false;
    }
}
