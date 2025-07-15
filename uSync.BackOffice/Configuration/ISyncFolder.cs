namespace uSync.BackOffice.Configuration;

/// <summary>
///  folder type to define a folder for the site. 
/// </summary>
public interface ISyncFolder
{
    /// <summary>
    ///  path to the folder we want to use. 
    /// </summary>
    string Path { get; }

    /// <summary>
    ///  weight in the list. 
    /// </summary>
    int Weight { get; }
}



/*
public class TestSyncFolder: ISyncFolder
{
    public string Path => "uSync/TestFolder";
    public int Weight => 100;
}
*/