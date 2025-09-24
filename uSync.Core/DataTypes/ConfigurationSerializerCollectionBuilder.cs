using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.DataTypes
{
    public class ConfigurationSerializerCollectionBuilder
        : LazyCollectionBuilderBase<ConfigurationSerializerCollectionBuilder, ConfigurationSerializerCollection, IConfigurationSerializer>
    {
        protected override ConfigurationSerializerCollectionBuilder This => this;
    }


    public class ConfigurationSerializerCollection :
        BuilderCollectionBase<IConfigurationSerializer>
    {
        private readonly ILogger<ConfigurationSerializerCollection> _logger;

        [Obsolete("Use constructor with logger will be removed in 18")]
        public ConfigurationSerializerCollection(Func<IEnumerable<IConfigurationSerializer>> items)
            : base(items) 
        { }

        public ConfigurationSerializerCollection(Func<IEnumerable<IConfigurationSerializer>> items,
            ILogger<ConfigurationSerializerCollection> logger)
            : base(items)
        {
            _logger = logger;
        }

        public IConfigurationSerializer GetSerializer(string editorAlias)
            => this.FirstOrDefault(x => x.Editors.InvariantContains(editorAlias));

        public object DeserializeConfig(string editorAlias, string config, Type configType)
        {
            try
            {
                var serializer = GetSerializer(editorAlias);
                if (serializer != null)
                    return serializer.DeserializeConfig(config, configType);

                return JsonConvert.DeserializeObject(config, configType);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing config for editor {editorAlias} and type {configType}", editorAlias, configType);
                throw; 
            }
        }

        public string SerializeConfig(string editorAlias, object configuration, JsonSerializerSettings serializerSettings)
        {
            try
            {
                var serializer = GetSerializer(editorAlias);
                if (serializer != null)
                    return serializer.SerializeConfig(configuration);

                return JsonConvert.SerializeObject(configuration, Formatting.Indented, serializerSettings);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error serializing config for editor {editorAlias} and config type {configType}", editorAlias, configuration.GetType());
                throw; 
            }
        }
    }
}
