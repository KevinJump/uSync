using System.Runtime.Serialization;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

[NullableMapper]
public class MultiUrlMapper : SyncValueMapperBase, ISyncMapper
{
    public MultiUrlMapper(IEntityService entityService) : base(entityService)
    {
    }

    public override string Name => "MultiUrl Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.MultiUrlPicker
    ];

    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        if (value.ToString().TryDeserialize<List<LinkDto>>(out var links) is false || links is null || links.Count == 0)
            return [];

        return links.Where(x => x.Udi != null)?
            .Select(link => CreateDependency(link.Udi, flags)) ?? [];
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
