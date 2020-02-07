using System.Collections.Generic;
using Umbraco.Core;

namespace uSync8.BackOffice.Commands
{
    /// <summary>
    ///  command options, used when we parse the command line. 
    /// </summary>
    /// <remarks>
    ///  Command structure is fairly simple when we parse things 
    ///  the first non-switched command will always be the folder,
    ///  the rest come from switches.
    ///     
    ///  If the switch doesn't have a value it will be set to true.
    ///  
    ///  .e.g.
    ///     -force  => 'force', true
    ///     -set=mysetname => 'set', 'mysetname'
    /// </remarks>
    public class SyncCommandOptions
    {
        public string Folder { get; set; }
        public Dictionary<string, object> Switches { get; set; }
            = new Dictionary<string, object>();

        public SyncCommandOptions(string folder)
        {
            this.Folder = folder;
            
        }

        /// <summary>
        ///  get a switch value from the switches list,
        /// </summary>
        /// <returns>Defaultvalue if not set, or of expect type</returns>
        public TObject GetSwitchValue<TObject>(string name, TObject defaultValue)
        {
            if (Switches.ContainsKey(name))
            {
                var result = Switches[name].TryConvertTo<TObject>();
                if (result.Success) return result.Result;
            }

            return defaultValue;
        }
    }
}
