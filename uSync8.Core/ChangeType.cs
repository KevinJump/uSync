namespace uSync8.Core
{

    public enum ChangeType
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
        Fail = 11,
        ImportFail,
        Mismatch
    }

}
