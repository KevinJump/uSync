namespace uSync.BackOffice.Models;

/// <summary>
///  An add on to uSync, which allows you to inject a view onto the uSync page
///  just like a content app. 
/// </summary>
public interface ISyncAddOn
{
    /// <summary>
    /// Name used listing what is installed
    /// </summary>
    string Name { get; }


    /// <summary>
    /// your own version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// icon to use for app
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// view (if this is blank, the add on will not show in the top right)
    /// </summary>
    string View { get; }

    /// <summary>
    /// alias for the 'app'
    /// </summary>
    string Alias { get; }

    /// <summary>
    /// name shown under the icon.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///  sort order - the lower the further up the list you will be. 
    /// </summary>
    int SortOrder { get; }
}
