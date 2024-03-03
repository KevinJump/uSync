using Umbraco.Cms.Core;

namespace uSync.Core.Dependency;

public delegate void uSyncDependencyUpdate(DependencyMessageArgs e);

/// <summary>
///  A Dependency message (sent to UI)
/// </summary>
public class DependencyMessageArgs
{
    /// <summary>
    ///  message about the dependency 
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///  number of dependencies
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///  total number of dependencies. 
    /// </summary>
    public int Total { get; set; }
}

public class uSyncDependency
{

    /// <summary>
    ///  name to display to user (not critical for deployment of a dependency)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///  the Umbraco Unique Id for the dependency
    /// </summary>
    public Udi? Udi { get; set; }  

    /// <summary>
    ///  order in which dependency will be imported
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///  the level (from root) for this dependency
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///  the mode (reserved)
    /// </summary>
    public DependencyMode Mode { get; set; }

    /// <summary>
    ///  dependency flags used on this item
    /// </summary>
    public DependencyFlags Flags { get; set; }

    /// <summary>
    ///  delegate message for sending UI updated
    /// </summary>

    public static event uSyncDependencyUpdate? DependencyUpdate;

    /// <summary>
    ///  send an update to the UI
    /// </summary>
    /// <param name="message"></param>
    public static void FireUpdate(string message)
    {
        FireUpdate(message, 1, 2);
    }

    /// <summary>
    ///  fires the UI update 
    /// </summary>
    private static void FireUpdate(string message, int count, int total)
    {
        DependencyUpdate?.Invoke(new DependencyMessageArgs
        {
            Message = message,
            Count = count,
            Total = total
        });
    }
}

/// <summary>
///  the match mode for the dependency (reserved)
/// </summary>
public enum DependencyMode
{
    MustMatch,
    MustExist
}
