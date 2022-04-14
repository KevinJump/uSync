using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace uSync.SchemaGenerator
{
    internal class Options
    {
        [Option('o', "outputFile", Required = false,
            HelpText = "",
            Default = "..\\uSync.Backoffice.Assets\\App_Plugins\\uSync\\config\\appsettings-usync-schema.json")]
        public string OutputFile { get; set; }
    }
}
