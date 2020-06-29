namespace uSync8.BackOffice
{
    public static partial class uSyncBackOfficeConstants
    {
        /// <summary>
        ///  priorities determain the order things get imported in.
        /// </summary>
        /// <remarks>
        ///  In general: 
        ///    - datatypes have to be before everything
        ///    - contentTypes/mediatypes/etc require datatypes and templates
        ///    - media/content need everthing setup
        ///    - domains and relations are at the end. 
        ///    
        ///  custom things should go for numbers passed USYNC_RESERVED_UPPER
        ///  to ensure they happen at the end. 
        ///     
        ///  Where there are circular dependencies (e.g doctypes that refrence
        ///  content structures). We have post processing, in that things will
        ///  be called again at the end of the process (after everything else) 
        /// </remarks>
        public static class Priorites
        {
            public const int USYNC_RESERVED_LOWER = 1000;
            public const int USYNC_RESERVED_UPPER = 2000;

            public const int DataTypes = USYNC_RESERVED_LOWER + 10;
            public const int Templates = USYNC_RESERVED_LOWER + 20;

            public const int ContentTypes = USYNC_RESERVED_LOWER + 30;
            public const int MediaTypes = USYNC_RESERVED_LOWER + 40;
            public const int MemberTypes = USYNC_RESERVED_LOWER + 45;

            public const int Languages = USYNC_RESERVED_LOWER + 5;
            public const int DictionaryItems = USYNC_RESERVED_LOWER + 6;
            public const int Macros = USYNC_RESERVED_LOWER + 70;

            public const int Media = USYNC_RESERVED_LOWER + 200;
            public const int Content = USYNC_RESERVED_LOWER + 210;
            public const int ContentTemplate = USYNC_RESERVED_LOWER + 215;

            public const int DomainSettings = USYNC_RESERVED_LOWER + 219;

            public const int RelationTypes = USYNC_RESERVED_LOWER + 220;

            public const int DataTypeMappings = USYNC_RESERVED_LOWER + 250;
        }

        /// <summary>
        ///  Sync groups for handlers when only doing 'part' sync
        /// </summary>
        /// <remarks>
        ///  If the handler doesn't speficiy a group its treated as being part 
        ///  of the 'settings' group.
        /// 
        ///  Handlers are not limited to these groups, but if you have a custom
        ///  one the UI will look for language keys (in the xml files)
        ///  
        ///    usync_import-[name] and usync_report-[name]
        ///    
        /// </remarks>

        public static class Groups
        {
            public const string Settings = "Settings";
            public const string Content = "Content";

            public const string Members = "Members";
            public const string Users = "Users";

        }
    }
}
