using CommandLine;

namespace uSync
{
    internal class Options
    {
        [Option('o', "outputFile", Required = false,
            HelpText = "",
            Default = "..\\uSync.Backoffice.Targets\\appsettings-schema.usync.json")]
        public string OutputFile { get; set; }

        [Option('s', "site", Required = false,
            HelpText = "The test site to also copy the file to.",
            Default = "uSync.Site")]
        public string Site { get; set; } 
    }
}
