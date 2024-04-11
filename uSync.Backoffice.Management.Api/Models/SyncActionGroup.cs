namespace uSync.Backoffice.Management.Api.Models;

public class SyncActionGroup
{
    public required string Key { get; init; }
    public string Icon { get; set; } = "icon-box";
    public string GroupName { get; set; } = "settings";
    public List<SyncActionButton> Buttons { get; set; } = [];

}

public class SyncActionButton
{
    public string Key { get; set; } = "";

    public string Label { get; set; } = string.Empty;

    public string Look { get; set; } = "primary";
    public string Color { get; set; } = "default";

    public bool Force { get; set; } = false;
    public bool Clean { get; set;} = false;

    public List<SyncActionButton> Children { get; set; } = [];
}
