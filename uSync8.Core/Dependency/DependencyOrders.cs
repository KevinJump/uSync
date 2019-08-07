using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Umbraco.Core.Constants;

namespace uSync8.Core.Dependency
{
    /// <summary>
    ///  The base order things go in the dependency tree,
    ///  there can be variations, so for example compositions
    ///  will have a lower priority then the content type they 
    ///  belong to. 
    /// </summary>
    public static class DependencyOrders
    {
        public static int Languages = 10;
        public static int DictionaryItems = 20;

        public static int Templates = 100;
        public static int DataTypes = 200;

        // lower bound for compositions.
        //  in reality we start at contentTypes 
        //  and go down one for each level
        public static int Compositions = 400;


        public static int ContentTypes = 500;
        public static int MediaTypes = 600;
        public static int MemberTypes = 700;

        public static int Macros = 800;

        public static int Media = 900;
        public static int Content = 1000;

        public static int OrderFromEntityType(string entityType)
        {
            switch(entityType)
            {
                case UdiEntityType.Document:
                    return Content;
                case UdiEntityType.Media:
                    return Media;
                case UdiEntityType.DataTypeContainer:
                case UdiEntityType.DataType:
                    return DataTypes;
                case UdiEntityType.DocumentType:
                case UdiEntityType.DocumentTypeContainer:
                    return ContentTypes;
                case UdiEntityType.MediaType:
                case UdiEntityType.MediaTypeContainer:
                    return MediaTypes;
                case UdiEntityType.DictionaryItem:
                    return DictionaryItems;
                case UdiEntityType.Template:
                    return Templates;
                case UdiEntityType.Macro:
                    return Macros;
                case UdiEntityType.Language:
                    return Languages;
                default:
                    return 2000;
            }
        }
    }
}
