using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace uSync.Core.Mapping
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
            Constants.PropertyEditors.Aliases.MultipleTextstring
        };

        public override string GetImportValue(string value, string editorAlias)
        {
            if (value.IsValidJsonString() is true)
            {
                try
                {
                    var result = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<object>(value));
                    return result;
                }
                catch
                {
                    return value;
                }
            }


            if (!value.Contains('\r'))
            {
                return value.Replace("\n", "\r\n");
            }

            return value;
        }
    }
}
