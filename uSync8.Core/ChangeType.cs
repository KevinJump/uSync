namespace uSync8.Core
{

    public enum ChangeType : int
    {
        Removed = -1,
        NoChange = 0,
        Create,
        Import,
        Export,
        Update,
        Delete,
        WillChange,
        Information,
        Rolledback,
        Clean,
        Fail = 11,
        ImportFail,
        Mismatch
    }

}
