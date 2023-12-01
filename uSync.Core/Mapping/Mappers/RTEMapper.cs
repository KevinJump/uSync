using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
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
        private readonly Lazy<SyncValueMapperCollection> mapperCollection;

        public RTEMapper(
            IEntityService entityService,
            Lazy<SyncValueMapperCollection> mappers)
            : base(entityService)
        {
            this.mapperCollection = mappers;
        }

        // would prefere the link regex - less likely to get rouge ones 
        // private string linkRegEx = "((?&lt;=localLink:)([0-9]+)|(?&lt;=data-id=&quot;)([0-9]+))";
        private Regex UdiRegEx = new Regex(@"(umb:[/\\]+[a-zA-Z-]+[/\\][a-zA-Z0-9-]+)",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private Regex MacroRegEx = new Regex("<\\?UMBRACO_MACRO[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

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

            if (stringValue.TryParseValidJsonString(out JObject jObject))
            {
                // if its json, it contains the new blocks way of sending shizzel. 
                return GetBlockDependencies(jObject, editorAlias, flags);
            }

            return GetSimpleDependencies(stringValue, editorAlias, flags);
        }

        private IEnumerable<uSyncDependency> GetBlockDependencies(JObject jObject, string editorAlias, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();

            if (jObject.TryGetValue("markup", out JToken markup) && markup != null) { 
                dependencies.AddRange(GetSimpleDependencies(markup.ToString(), editorAlias, flags));
            }

            if (jObject.TryGetValue("blocks", out JToken blocks) && blocks != null) {
                dependencies.AddRange(mapperCollection.Value.GetDependencies(blocks, Constants.PropertyEditors.Aliases.BlockList, flags));
            }

            return dependencies;
        }

        private IEnumerable<uSyncDependency> GetSimpleDependencies(string stringValue, string editorAlias, DependencyFlags flags)
        {
            if (string.IsNullOrWhiteSpace(stringValue)) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (Match m in UdiRegEx.Matches(stringValue))
            {
                if (UdiParser.TryParse<GuidUdi>(m.Value, out GuidUdi udi))
                {
                    if (!dependencies.Any(x => x.Udi == udi))
                        dependencies.Add(CreateDependency(udi, flags));
                }
            }

            if (MacroRegEx.IsMatch(stringValue))
            {
                var mappers = mapperCollection.Value.GetSyncMappers(editorAlias + ".macro");
                if (mappers.Any())
                {
                    foreach (var macro in MacroRegEx.Matches(stringValue))
                    {
                        foreach (var mapper in mappers)
                        {
                            dependencies.AddRange(mapper.GetDependencies(stringValue, editorAlias + ".macro", flags));
                        }
                    }
                }
            }

            return dependencies.Distinct();
        }
    }
}
