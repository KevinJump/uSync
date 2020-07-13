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

            public const string Dictionary = "Dictionary";
            public const string Content = "Content";
            public const string Media = "Media";
            public const string Users = "Users";
            public const string Members = "Members";

            public const string Domain = "Domain";

            public const string Empty = "Empty";

            public const string RelationType = "RelationType";
            public const string Relation = "Relation";
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
    }


}
