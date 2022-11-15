using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Build.Framework;

public class InsertuSyncSchemaReference : Microsoft.Build.Utilities.Task
{
    /// <summary>
    ///  The section name you want to add to the schema file 
    /// </summary>
    [Required]
    public string? Name { get; set; }

    /// <summary>
    ///  The refenrence usally this follows the form filename#/definitions/setupsection
    /// </summary>
    [Required]
    public string? Reference { get; set; }

    /// <summary>
    ///  the target file (almost always appsettings-schema.json)
    /// </summary>
    [Required]
    public string? TargetFile { get; set; }

    /// <summary>
    ///  run the task. 
    /// </summary>
    /// <returns></returns>
    public override bool Execute()
    {
        if (!File.Exists(TargetFile))
        {
            Log.LogWarning("Cannot find target schema file [" + TargetFile + "]");
            return true;
        }

        return LoadJson(TargetFile);
    }


    private bool LoadJson(string file)
    {
        try
        {
            var contents = File.ReadAllText(file, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(contents))
                throw new InvalidDataException("No contents of schema file");

            JsonNode? node = JsonNode.Parse(contents);
            if (node == null)
                throw new InvalidDataException("Bad JSON in schema file");

            var properties = node["properties"];
            if (properties is null)
                throw new InvalidDataException("Missing 'Properties' node in schema");

            if (properties[Name] != null) return true; // already there ?

            properties[Name] = new JsonObject
            {
                ["$ref"] = Reference
            };

            File.WriteAllText(file,
                node.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            );
        }
        catch (Exception ex)
        {
            // Log a warning shouldn't be a showstopper its nice to have.
            Log.LogWarning($"Failed to update schema with {Name} references {ex.Message}");
        }

        return true;
    }
}
