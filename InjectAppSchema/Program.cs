using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.IO;

namespace InjectAppSchema
{
    class Program
    {
        /// <summary>
        ///  "simple" program to inject a some json into the appsettings schema 
        ///  of an umbraco project 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            if (args.Length != 3)
            {
                Console.WriteLine("Not enough arguments, InjectAppSchema [name] [Source] [target]");
                return;
            }

            var name = args[0];
            var source = Path.GetFullPath(args[1]);
            var target = Path.GetFullPath(args[2]);

            if (!File.Exists(source))
            {
                Console.WriteLine($"Cannot find source '{source}'");
                return;
            }

            if (!File.Exists(target))
            {
                Console.WriteLine($"Cannot find target '{target}'");
                return;
            }


            var json = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(target));

            var properties = json["properties"];
            if (properties != null)
            {

                if (properties[name] != null)
                {
                    Console.WriteLine($"Property '{name}' already in schema");
                    return;
                }

                var refString = GetRefString(name, source, target); 

                properties[name] = new JObject();
                properties[name]["$ref"] = refString;

                Console.WriteLine($">> Added '{refString}' to Json");
            }

            File.WriteAllText(target, JsonConvert.SerializeObject(json, Formatting.Indented));
            Console.WriteLine($">> Saved {target}");
          
        }

        static string GetRefString(string name, string source, string target)
        {
            var relative = Path.GetRelativePath(Path.GetDirectoryName(target), source).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return $"{relative}#/definitions/{name}";
        }
    }
}
