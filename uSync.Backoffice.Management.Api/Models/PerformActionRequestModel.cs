namespace uSync.Backoffice.Management.Api.Models;


public record PerformActionRequestModel(
    string RequestId,
    string GroupName,
    string ActionName,
    int StepNumber);


public record PerformActionResponse(
    string RequestId,
    bool Completed,
    List<ActionInfo> ActionInfo);


public record ActionInfo
{
    public required string ActionName { get; set; }
    public required string Icon { get; set; }
    public bool Completed { get; set; } = false;
    public bool Working { get; set; } = false;
}