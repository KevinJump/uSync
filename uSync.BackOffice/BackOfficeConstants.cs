using System;
using System.Collections.Generic;

namespace uSync.BackOffice
{
    public static partial class uSyncConstants
    {
        public const string ReleaseSuffix = "";

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

            public const int DataTypeMappings = USYNC_RESERVED_LOWER + 220;

            public const int RelationTypes = USYNC_RESERVED_LOWER + 230;
        }

        public static class Groups
        {
            public const string Settings = "Settings";
            public const string Content = "Content";

            public const string Members = "Members";
            public const string Users = "Users";

            public static Dictionary<string, string> Icons = new Dictionary<string, string> {
                { Settings, "icon-settings-alt color-blue" },
                { Content, "icon-documents color-purple" },
                { Members, "icon-users" },
                { Users, "icon-users color-green"}
            };
        }

        public static class Handlers
        {
            public const string ContentHandler = "ContentHandler";
            public const string ContentTemplateHandler = "ContentTemplateHandler";
            public const string ContentTypeHandler = "ContentTypeHandler";
            public const string DataTypeHandler = "DataTypeHandler";
            public const string DictionaryHandler = "DictionaryHandler";
            public const string DomainHandler = "DomainHandler";
            public const string LanguageHandler = "LanguageHandler";
            public const string MacroHandler = "MacroHandler";
            public const string MediaHandler = "MediaHandler";
            public const string MediaTypeHandler = "MediaTypeHandler";
            public const string MemberTypeHandler = "MemberTypeHandler";
            public const string RelationTypeHandler = "RelationTypeHandler";
            public const string TemplateHandler = "TemplateHandler";


        }
    }
}
