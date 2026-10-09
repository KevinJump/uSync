using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace uSync.Backoffice.Management.Api.Configuration;

/// <summary>
///  replaces the System.Exception schema in a swagger document with a simple object.
/// </summary>
/// <remarks>
///  uSyncAction.Exception is a System.Exception, so swashbuckle walks TargetSite
///  (MethodBase) and pulls the whole reflection graph (Assembly, Module, Type,
///  IntPtr...) into the spec.
///
///  Swagger gen options are shared by every document on a site, so rather than
///  MapType (which would change everyone's docs) this only touches the document
///  named by ApiName. Other packages exposing uSyncAction can inherit from this
///  for their own documents.
/// </remarks>
public abstract class SyncExceptionSchemaDocumentFilterBase : IDocumentFilter
{
    private const string ExceptionSchemaId = "Exception";

    protected abstract string ApiName { get; }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (context.DocumentName != ApiName) return;

        var schemas = swaggerDoc.Components?.Schemas;
        if (schemas is null || schemas.ContainsKey(ExceptionSchemaId) is false) return;

        var reachableBefore = FindReachableSchemas(swaggerDoc, schemas);

        schemas[ExceptionSchemaId] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["stackTrace"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["source"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["helpLink"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["hResult"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
            }
        };

        // only remove the schemas the old exception schema was keeping alive.
        var reachableAfter = FindReachableSchemas(swaggerDoc, schemas);
        foreach (var id in reachableBefore.Except(reachableAfter))
            schemas.Remove(id);
    }

    private static HashSet<string> FindReachableSchemas(OpenApiDocument doc, IDictionary<string, IOpenApiSchema> schemas)
    {
        var found = new HashSet<string>();
        var stack = new Stack<IOpenApiSchema?>();

        foreach (var path in doc.Paths.Values)
        {
            foreach (var parameter in path.Parameters ?? [])
                stack.Push(parameter.Schema);

            if (path.Operations is null) continue;

            foreach (var operation in path.Operations.Values)
            {
                foreach (var parameter in operation.Parameters ?? [])
                    stack.Push(parameter.Schema);

                foreach (var content in operation.RequestBody?.Content?.Values ?? [])
                    stack.Push(content.Schema);

                if (operation.Responses is null) continue;

                foreach (var response in operation.Responses.Values)
                    foreach (var content in response.Content?.Values ?? [])
                        stack.Push(content.Schema);
            }
        }

        while (stack.Count > 0)
        {
            var schema = stack.Pop();
            if (schema is null) continue;

            if (schema is OpenApiSchemaReference reference)
            {
                var id = reference.Reference.Id;
                if (id is not null && found.Add(id) && schemas.TryGetValue(id, out var target))
                    stack.Push(target);
                continue;
            }

            stack.Push(schema.Items);
            stack.Push(schema.AdditionalProperties);
            stack.Push(schema.Not);
            foreach (var child in schema.Properties?.Values ?? []) stack.Push(child);
            foreach (var child in schema.AllOf ?? []) stack.Push(child);
            foreach (var child in schema.AnyOf ?? []) stack.Push(child);
            foreach (var child in schema.OneOf ?? []) stack.Push(child);
        }

        return found;
    }
}
