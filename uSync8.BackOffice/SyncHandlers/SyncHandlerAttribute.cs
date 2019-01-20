using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerAttribute : Attribute
    {
        public SyncHandlerAttribute(string id, string name, string folder, int priority)
        {
            Id = new Guid(id);
            Name = name;
            Priority = priority;
            Folder = folder;
        }

        public string Name { get; set; }
        public Guid Id { get; set; }
        public int Priority { get; set; }
        public string Folder { get; set; }
        public bool TwoStep { get; set; } = false;
    }
}
