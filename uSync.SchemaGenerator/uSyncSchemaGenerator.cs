using Namotion.Reflection;

using NJsonSchema;
using NJsonSchema.Generation;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace uSync;

internal class uSyncSchemaGenerator : JsonSchemaGenerator
{
    public uSyncSchemaGenerator()
        : base(new SystemTextJsonSchemaGeneratorSettings()
        {
            AlwaysAllowAdditionalObjectProperties = true,
            FlattenInheritanceHierarchy = true,
            IgnoreObsoleteProperties = true,
            ReflectionService = new UmbracoSystemTextJsonReflectionService(),
            SerializerOptions = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() },
                IgnoreReadOnlyProperties = true,
            },
            DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
            SchemaNameGenerator = new NamespacePrefixedSchemaNameGenerator(),
            GenerateExamples = true,
        })
    { }
}

internal class uSyncSchemaGeneratorSettings : SystemTextJsonSchemaGeneratorSettings
{
    public uSyncSchemaGeneratorSettings()
    {
        AlwaysAllowAdditionalObjectProperties = true;
        IgnoreObsoleteProperties = true;
        SerializerOptions = new JsonSerializerOptions()
        {
            Converters = { new JsonStringEnumConverter() },
            IgnoreReadOnlyProperties = true,
        };
        ReflectionService = new UmbracoSystemTextJsonReflectionService();
        DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
        SchemaNameGenerator = new NamespacePrefixedSchemaNameGenerator();
        GenerateExamples = true;
    }
}
    internal class UmbracoSystemTextJsonReflectionService : SystemTextJsonReflectionService
    {
        /// <inheritdoc />
        public override void GenerateProperties(JsonSchema schema, ContextualType contextualType, SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            // Populate schema properties
            base.GenerateProperties(schema, contextualType, settings, schemaGenerator, schemaResolver);

            if (settings.SerializerOptions.IgnoreReadOnlyProperties)
            {
                // Remove read-only properties (because this is not implemented by the base class)
                foreach (ContextualPropertyInfo property in contextualType.Properties)
                {
                    if (property.CanWrite is false)
                    {
                        string propertyName = GetPropertyName(property, settings);

                        schema.Properties.Remove(propertyName);
                    }
                }
            }
        }
    }


internal class NamespacePrefixedSchemaNameGenerator : DefaultSchemaNameGenerator
{
    public override string Generate(Type type) => type.Namespace.Replace(".", string.Empty) + base.Generate(type);
}
