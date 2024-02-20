namespace uSync.Core.Serialization;

public interface ISyncCachedSerializer
{
    void InitializeCache();

    void DisposeCache();
}
