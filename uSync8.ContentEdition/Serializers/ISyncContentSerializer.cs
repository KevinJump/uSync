namespace uSync8.ContentEdition.Serializers
{
    public interface ISyncContentSerializer<TObject>
    {
        string GetItemPath(TObject item);
    }
}
