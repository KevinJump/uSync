namespace uSync.BackOffice.SyncHandlers.Models;

/// <summary>
///  options that define how we define a handler 
/// </summary>
public class SyncHandlerOptions
{
    /// <summary>
    ///  Handler grouping (settings, content, etc) 
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    ///  What action do you want to perform.
    /// </summary>
    public HandlerActions Action { get; set; } = HandlerActions.None;

    /// <summary>
    ///  handler set
    /// </summary>
    public string Set { get; set; } = uSync.Sets.DefaultSet;

    /// <summary>
    ///  include handlers that are by default disabled 
    /// </summary>
    public bool IncludeDisabled { get; set; } = false;

    /// <summary>
    ///  the user id doing all the work.
    /// </summary>
    /// <remarks>
    ///  left as <c>Constants.Security.UnknownUserId</c>, uSync uses the current backoffice
    ///  user, then the configured background user, then the superuser.
    /// </remarks>
    public int UserId { get; set; } = Umbraco.Cms.Core.Constants.Security.UnknownUserId;

    /// <summary>
    /// Default constructor
    /// </summary>
    public SyncHandlerOptions() { }

    /// <summary>
    /// Construct Options for a given set
    /// </summary>
    public SyncHandlerOptions(string setName)
        : this()
    {
        Set = setName;
    }

    /// <summary>
    ///  construct for a given site and userId
    /// </summary>
    public SyncHandlerOptions(string setName, int userId)
        : this(setName)
    {
        UserId = userId;
    }

    /// <summary>
    /// Construct options with set and handler action set.
    /// </summary>
    public SyncHandlerOptions(string setName, HandlerActions action)
        : this(setName)
    {
        Set = setName;
        Action = action;
    }

    /// <summary>
    ///  construct for a given set, action and userId
    /// </summary>
    public SyncHandlerOptions(string setName, HandlerActions action, int userId)
        : this(setName, action)
    {
        UserId = userId;
    }
}
