using CommandLine;

namespace uSync;

internal sealed class Options
{
    [Option('o', "outputFile", Required = false,
        HelpText = "",
        Default = "..\\uSync.Backoffice.Targets\\appsettings-schema.usync.json")]
    public string OutputFile { get; set; } = "..\\uSync.Backoffice.Targets\\appsettings-schema.usync.json";
}
