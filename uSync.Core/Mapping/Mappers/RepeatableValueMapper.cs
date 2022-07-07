﻿using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

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
            if (!value.Contains('\r'))
            {
                return value.Replace("\n", "\r\n");
            }

            return value;
        }
    }
}
