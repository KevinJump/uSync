using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class MultiUrlMapper : SyncValueMapperBase, ISyncMapper
    {
        public MultiUrlMapper(IEntityService entityService) : base(entityService)
        {
        }

        public override string Name => "MultiUrl Mapper";

        public override string[] Editors => new string[] {
            Constants.PropertyEditors.Aliases.MultiUrlPicker
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var links = JsonConvert.DeserializeObject<IEnumerable<LinkDto>>(value.ToString());

            var dependencies = new List<uSyncDependency>();

            foreach (var link in links)
            {
                if (link.Udi != null)
                {
                    dependencies.Add(CreateDependency(link.Udi, flags));
                }
            }

            return dependencies;
        }

        // taken from umbraco source - this is how it's stored 
        // we need to just be able to read / manipulate the storage 
        // to make things generic .
        [DataContract]
        internal class LinkDto
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "target")]
            public string Target { get; set; }

            [DataMember(Name = "udi")]
            public GuidUdi Udi { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }

            [DataMember(Name = "queryString")]
            public string QueryString { get; set; }
        }
    }
}
