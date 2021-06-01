using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;

using uSync8.Core.Dependency;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class MediaPicker3Mapper : SyncValueMapperBase, ISyncMapper
    {
        public MediaPicker3Mapper(IEntityService entityService) : base(entityService)
        { }

        public override string Name => "MediaPicker3 Mapper";

        public override string[] Editors => new string[]
        {
            "Umbraco.MediaPicker3"
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            // validate string 
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson())
                return Enumerable.Empty<uSyncDependency>();

            // convert to an array. 
            var images = JsonConvert.DeserializeObject<JArray>(value.ToString());
            if (images == null || !images.Any())
                return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach(var image in images.Cast<JObject>())
            {
                var key = GetGuidValue(image, "mediaKey");

                if (key != Guid.Empty)
                {
                    var udi = GuidUdi.Create(UdiEntityType.Media, key);
                    dependencies.Add(CreateDependency(udi as GuidUdi, flags));
                }
            }

            return dependencies;
        }

        private Guid GetGuidValue(JObject obj, string key)
        {
            if (obj != null && obj.ContainsKey(key))
            {
                var attempt = obj[key].TryConvertTo<Guid>();
                if (attempt.Success)
                    return attempt.Result;
            }

            return Guid.Empty;

        }
    }
}
