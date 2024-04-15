namespace uSync.Core;

public static partial class uSyncConstants
{
    public static class Xml
    {
        public const string Key = "Key";
        public const string Alias = "Alias";
        public const string Level = "Level";
        public const string Change = "Change";

        public const string Parent = "Parent";
        public const string Path = "Path";

        public const string Info = "Info";
        public const string Name = "Name";

        public const string SortOrder = "SortOrder";

        public const string ItemType = "itemType";

        public const string Template = "Template";

    }

    // this is our format 'version' - 
    // it only changes when the format of the .config files change
    // we use it to prompt people to do an uptodate export.
    public const string FormatVersion = "10.7.0";

    /// <summary>
    ///  names of the root xml elements that are seralized in/out
    /// </summary>
    public static class Serialization
    {
        public const string ContentType = "ContentType";
        public const string MediaType = "MediaType";
        public const string DataType = "DataType";
        public const string MemberType = "MemberType";
        public const string Template = "Template";
        public const string Macro = "Macro";
        public const string Language = "Language";

        public const string Dictionary = "Dictionary";
        public const string Content = "Content";
        public const string Media = "Media";
        public const string Users = "Users";
        public const string Members = "Members";

        public const string Domain = "Domain";

        public const string Empty = "Empty";

            public const string RelationType = "RelationType";
            public const string Relation = "Relation";

            public const string Webhook = "Webhook";
        }

    /// <summary>
    ///  Key used in settings and xml to indicate only partial cultures are included in file
    /// </summary>
    public const string CultureKey = "Cultures";

    /// <summary>
    ///  Key used in settings and in xml to indicate only partial segments are included in file
    /// </summary>
    public const string SegmentKey = "Segments";

    /// <summary>
    ///  Key used in settings and in xml to indicate if partial file also includes fallback values.
    /// </summary>
    public const string DefaultsKey = "DefaultValues";


    /// <summary>
    ///  constant values for default settings in handlers/serializers.
    /// </summary>
    /// <remarks>
    ///  these are checked within the serializers if the config is not present the default values
    ///  are used.
    /// </remarks>
    public static class DefaultSettings
    {
        // don't remove properties or items (e.g no delete)
        public const string NoRemove = "NoRemove";
        public const bool NoRemove_Default = false;

        // only create new items (existing items are not touched)
        public const string CreateOnly = "CreateOnly";
        public const string OneWay = "OneWay"; // legacy config name 
        public const bool CreateOnly_Default = false;

        // new properties only (so existing properties are left)
        public const string NewPropertiesOnly = "NewPropertiesOnly";
        public const bool NewPropertiesOnly_Default = false;

        // only publish content when we thing it is dirty 
        public const string OnlyPublishDirty = "OnlyPublishDirty";
        public const bool OnlyPublishDirty_Default = false;

        // editors we don't want the config importing for.
        public const string NoConfigEditors = "NoConfigEditors";
        public const string NoConfigEditors_Default = "";

        // item names we don't want the config importing for. 
        public const string NoConfigNames = "NoConfigNames";
        public const string NoConfigNames_Default = "";

    }

    public const int DependencyCountMax = 204800;


    public static class Conventions
    {
        /// <summary>
        ///  setting to tell a serializer to include the file content in anything it is sending over. 
        /// </summary>
        public const string IncludeContent = "IncludeContent";
    }
}

/// <summary>
///  legacy property editor names, 
/// </summary>
/// <remarks>
///  saved for prosperity should they be removed from the core. 
/// </remarks>
public static class SyncLegacyTypes
{
    public const string NestedContent = "Umbraco.NestedContent";
    public const string OurNestedContent = "Our.Umbraco.NestedContent";
    public const string Grid = "Umbraco.Grid";
    public const string MediaPicker = "Umbraco.MediaPicker";
}
