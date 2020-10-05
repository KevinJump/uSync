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

        Task<SyncCommandResult> Run(string[] args);

        Task ShowHelp(bool advanced);

        bool Interactive { get; set; }
    }
}
