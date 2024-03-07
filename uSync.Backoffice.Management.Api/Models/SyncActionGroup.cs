namespace uSync.Backoffice.Management.Api.Models;

public class SyncActionGroup
{
    public string Key { get; set; } = "";
    public string Icon { get; set; } = "icon-box";
    public string GroupName { get; set; } = "settings";
    public List<SyncActionButton> Buttons { get; set; } = [];

}

public class SyncActionButton
{
    public string Key { get; set; } = "";
    public string Look { get; set; } = "primary";
    public string Color { get; set; } = "default";
}
