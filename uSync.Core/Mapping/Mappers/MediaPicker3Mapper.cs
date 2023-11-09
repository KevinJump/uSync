using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core.Mapping.Mappers
{
    public class MediaPicker3Mapper : SyncValueMapperBase, ISyncMapper
    {
        public MediaPicker3Mapper(IEntityService entityService) : base(entityService)
        { }

        public override string Name => "MediaPicker3 Mapper";

        public override string[] Editors => new string[]
        {
            Constants.PropertyEditors.Aliases.MediaPicker3
        };

        public override string GetExportValue(object value, string editorAlias)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrEmpty(stringValue) is true) return null;

            if (stringValue.TryParseValidJsonString(out JArray json) is false)
                return stringValue;

            // re-formatting the json in the picker.
            // 
            // we do this because sometimes (and with the starter kit especially)
            // the json might have extra spaces in it, so compared with a server 
            // where this has been imported vs created it can be different but the same.

            // by reading in the json and then spitting it out again, we remove any
            // rouge spacing - so our compare fires through as if nothing has changed.

            try
            {
                return JsonConvert.SerializeObject(json, Formatting.Indented);
            }
            catch
            {
                return stringValue;
            }
        }
            

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            // validate string 
            var stringValue = value?.ToString();
            if (!stringValue.TryParseValidJsonString(out JArray images) is false)
                return Enumerable.Empty<uSyncDependency>();

            if (images == null || !images.Any())
                return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var image in images.Cast<JObject>())
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
