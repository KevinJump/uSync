using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core
{
    public static partial class uSyncConstants
    {
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

            public const string Dicrionary = "Dicrionary";
            public const string Content = "Content";
            public const string Media = "Media";
            public const string Users = "Users";
            public const string Members = "Members";

            public const string Domain = "Domain";

            public const string Empty = "Empty";
        }
    }
}
