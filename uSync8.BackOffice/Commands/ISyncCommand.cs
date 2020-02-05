using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Commands
{
    /// <summary>
    ///  Implimenting ISyncCommand will make 
    ///  your code avalilbe to the command line tool.
    /// </summary>
    public interface ISyncCommand
    {
        string Name { get; }

        string Alias { get; }

        SyncCommandResult Run(string[] args);

        void ShowHelp(bool advanced);

        bool Interactive { get; set; }
    }
}
