namespace uSync.Core.Serialization
{
    public enum SerializerFlags
    {
        None = 0,
        Force = 1, // force the change (even if there isn't one)
        OnePass = 2, // do this in one pass
        DoNotSave = 4, // don't save 
        FailMissingParent = 8, // fail if the parent item is missing
        CreateOnly = 16 // only create, if the item is already there we don't overwrite.          
    }
}
