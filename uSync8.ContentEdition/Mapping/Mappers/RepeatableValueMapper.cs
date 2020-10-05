using System.Linq;

using Umbraco.Core.Services;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    /// <summary>
    ///  restore the /r/n to the string when the xml strips it out :( 
    /// </summary>
    public class RepeatableValueMapper : SyncValueMapperBase, ISyncMapper
    {
        public RepeatableValueMapper(IEntityService entityService)
            : base(entityService)
        { }

        public override string Name => "Repeatable Text Mapper";

        public override string[] Editors => new string[] {
            "Umbraco.MultipleTextstring"
        };

        public override string GetImportValue(string value, string editorAlias)
        {
            if (!value.Contains('\r'))
            {
                return value.Replace("\n", "\r\n");
            }

            return value;
        }
    }
}
