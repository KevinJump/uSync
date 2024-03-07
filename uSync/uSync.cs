namespace uSync;

/// <summary>
///  we only have this class, so there is a DLL in the root
///  uSync package.
///  
///  With a root DLL, the package can be stopped from installing
///  on .netframework sites.
/// </summary>
public static class uSync
{
    public static string PackageName = "uSync";
    // private static string Welcome = "uSync all the things";
}

// from v10.1 the package.manifest is enough for a file to appear in
// the list of installed packages 

// from v10.2 the version also comes from the package.manifest 
// so we can remove the package migration, as we don't need it now. 
