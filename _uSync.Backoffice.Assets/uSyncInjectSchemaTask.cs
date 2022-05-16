using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uSync.BackOffice.StaticAssets
{
    public class uSyncInjectSchemaTask : Task
    {
        [Required]
        public string RootFolder { get; set; }

        [Required]
        public string SourceSchema { get; set; }

        [Required]
        public string SchemaName { get; set; }

        [Required]
        public string SchemaDefinition { get; set; }

        public override bool Execute()
        {
            var targetSchema = Path.Combine(RootFolder, "umbraco", "config", "appsettings-schema.json");

            if (!File.Exists(SourceSchema))
            {
                Log.LogWarning($"Unable to locate source schema file {SourceSchema}");
                return true; // this isn't critical, we will pass but warn
            }

            if (!File.Exists(targetSchema))
            {
                Log.LogWarning($"Unable to locate target schema file {targetSchema}");
                return true;
            }

            // do the schema here.

            // var name = "uSync";
            // var reference = "#/definitions/USyncSchemaGeneratoruSyncDefinition";

            string jsonString = File.ReadAllText(targetSchema);
            var refString = GetRefString(SchemaName, SchemaDefinition, SourceSchema, targetSchema);

            var json = JsonConvert.DeserializeObject<JObject>(jsonString);
            if (json["properties"] != null)
{
                json["properties"]["uSync"] =
                    JObject.Parse($"{{ \"$ref\" : \"{refString}\" }}");

                var result = JsonConvert.SerializeObject(json, Formatting.Indented);

                File.WriteAllText(targetSchema, result);

                Log.LogMessage(MessageImportance.High, $"Added \"{SchemaName}\" to the Umbraco appsettings-schema.json file");
                return true;
            }

            Log.LogWarning("Unable to find properties element of json");
            return true;
        }

        private static string GetRefString(string name, string reference, string source, string target)
        {
            var relative = Path.GetRelativePath(Path.GetDirectoryName(target), source).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return $"{relative}{reference}";
        }
    }
}
