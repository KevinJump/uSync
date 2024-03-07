using System;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
///  Possible actions a handler can do (stored in config)
/// </summary>
public enum HandlerActions
{
    /// <summary>
    /// No Action
    /// </summary>
    [SyncActionName("")]
    None,

    /// <summary>
    /// Report action
    /// </summary>
    [SyncActionName("report")]
    Report,

    /// <summary>
    /// Import Action
    /// </summary>
    [SyncActionName("import")]
    Import,

    /// <summary>
    /// Export Action
    /// </summary>
    [SyncActionName("export")]
    Export,

    /// <summary>
    /// Save Action (triggered via Umbraco save/move events)
    /// </summary>
    [SyncActionName("save")]
    Save,

    /// <summary>
    /// All actions
    /// </summary>
    [SyncActionName("All")]
    All
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
internal class SyncActionName : Attribute
{
    private readonly string name;

    public SyncActionName(string name)
    {
        this.name = name;
    }

    public override string ToString()
        => this.name;
}
