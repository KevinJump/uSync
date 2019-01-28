using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Models
{
    /// <summary>
    ///  class to track changes of things inside 
    ///  umbraco, namely deletes, renames etc...
    /// </summary>
    public class SyncAction
    {
        public string TypeName { get; set; }
        public Guid Key { get; set; }
        public string Alias { get; set; }
        public SyncActionType Action { get; set; }
    }

    public enum SyncActionType
    {
        Delete, 
        Rename,
        Obsolete
    }
}
