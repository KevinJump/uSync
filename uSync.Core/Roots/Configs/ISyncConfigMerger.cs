namespace uSync.Core.Roots.Configs;

public interface ISyncConfigMerger
{
    string[] Editors { get; }

    object GetMergedConfig(string root, string target);
    object GetDifferenceConfig(string root, string target); 
}
