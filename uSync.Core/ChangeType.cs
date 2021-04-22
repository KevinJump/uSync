using System.Runtime.Serialization;

namespace uSync.Core
{
    /// <summary>
    ///  Type of change performed
    /// </summary>
    public enum ChangeType : int
    {
        [EnumMember(Value = "Clean")]
        Clean = -2,

        [EnumMember(Value = "Removed")]
        Removed = -1,

        [EnumMember(Value = "NoChange")]
        NoChange = 0,

        [EnumMember(Value = "Create")]
        Create,

        [EnumMember(Value = "Import")]
        Import,

        [EnumMember(Value = "Export")]
        Export,

        [EnumMember(Value = "Update")]
        Update,

        [EnumMember(Value = "Delete")]
        Delete,

        [EnumMember(Value = "WillChange")]
        WillChange,

        [EnumMember(Value = "Information")]
        Information,

        [EnumMember(Value = "Rolledback")]
        Rolledback,

        [EnumMember(Value = "Fail")]
        Fail = 11,

        [EnumMember(Value = "ImportFail")]
        ImportFail,

        [EnumMember(Value = "Mismatch")]
        Mismatch,

        [EnumMember(Value = "ParentMissing")]
        ParentMissing
    }

}
