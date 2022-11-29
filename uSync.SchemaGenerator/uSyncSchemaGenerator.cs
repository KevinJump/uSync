using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using NJsonSchema.Generation;

namespace uSync
{
    internal class uSyncSchemaGenerator
    {
        private readonly JsonSchemaGenerator _schemaGenerator;

        public uSyncSchemaGenerator()
        {
            _schemaGenerator = new JsonSchemaGenerator(
                new uSyncSchemaGeneratorSettings());
        }

        public string Generate() 
        {
            var uSyncSchema = GenerateuSyncSchema();
            return uSyncSchema.ToString();
        }

        private JObject GenerateuSyncSchema()
        {
            var schema = _schemaGenerator.Generate(typeof(AppSettings));
            return JsonConvert.DeserializeObject<JObject>(schema.ToJson());
        }
       
    }

    internal class uSyncSchemaGeneratorSettings : JsonSchemaGeneratorSettings
    {
        public uSyncSchemaGeneratorSettings()
        {
            AlwaysAllowAdditionalObjectProperties = true;
            SerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new WritablePropertiesOnlyResolver(),
            };
            DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
            SchemaNameGenerator = new NamespacePrefixedSchemaNameGenerator();
            SerializerSettings.Converters.Add(new StringEnumConverter());
            IgnoreObsoleteProperties = true;
            GenerateExamples  = true;
        }

        private class WritablePropertiesOnlyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                var result = props.Where(p => p.Writable).ToList();
                result.ForEach(x => x.PropertyName = ToPascalCase(x.PropertyName));
                return result;
            }

            /// <summary>
            ///  we serialize everything camel case inside uSync but the settings are actually PascalCase 
            ///  for appsettings.json, so we need to PascalCase each property. 
            /// </summary>
            private string ToPascalCase(string str)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    return char.ToUpperInvariant(str[0]) + str.Substring(1);
                }

                return str;

            }
        }
    }

    internal class NamespacePrefixedSchemaNameGenerator : DefaultSchemaNameGenerator
    {
        public override string Generate(Type type) => type.Namespace.Replace(".", string.Empty) + base.Generate(type);
    }
}
