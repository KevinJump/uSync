﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    /// <summary>
    ///  mapper for the tinyMCE editor 
    /// </summary>
    /// <remarks>
    /// Can be tricky because it contains embedded links 
    /// 
    /// "<p>Content Updated with a <a data-udi=\"umb://document/469b6e232ae04dcdb4a26e857f75e1fb\" href=\"/{localLink:umb://document/469b6e232ae04dcdb4a26e857f75e1fb}\" title=\"ContentTemplate\">link</a></p>" 
    /// </remarks>
    public class RTEMapper : SyncValueMapperBase, ISyncMapper
    {
        // would prefere the link regex - less likely to get rouge ones 
        // private string linkRegEx = "((?&lt;=localLink:)([0-9]+)|(?&lt;=data-id=&quot;)([0-9]+))";
        private Regex UdiRegEx = new Regex(@"(umb:[/\\]+[a-zA-Z-]+[/\\][a-zA-Z0-9-]+)",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private Regex MacroRegEx = new Regex("<\\?UMBRACO_MACRO[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public RTEMapper(IEntityService entityService) : base(entityService)
        {
        }

        public override string Name => "TinyMCE RTE Mapper";

        public override string[] Editors => new string[] {
            Constants.PropertyEditors.Aliases.TinyMce,
            $"{Constants.PropertyEditors.Aliases.Grid}.rte"
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            // value null check. 
            if (value == null) return Enumerable.Empty<uSyncDependency>();

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (Match m in UdiRegEx.Matches(stringValue))
            {
                if (GuidUdi.TryParse(m.Value, out GuidUdi udi))
                {
                    if (!dependencies.Any(x => x.Udi == udi))
                        dependencies.Add(CreateDependency(udi, flags));
                }
            }

            if (MacroRegEx.IsMatch(stringValue))
            {
                var macroMappers = SyncValueMapperFactory.GetMappers(editorAlias + ".macro");
                if (macroMappers.Any())
                {
                    foreach (var macroMapper in macroMappers)
                    {
                        foreach (var macro in MacroRegEx.Matches(stringValue))
                        {
                            dependencies.AddRange(macroMapper.GetDependencies(stringValue, editorAlias + ".macro", flags));
                        }
                    }
                }
            }

            return dependencies.Distinct();
        }
    }
}
