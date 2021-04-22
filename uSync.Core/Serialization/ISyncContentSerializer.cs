namespace uSync.Core.Serialization
{
    public interface ISyncContentSerializer<TObject>
    {
        string GetItemPath(TObject item);
    }
}
