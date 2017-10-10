using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XmlSerializerExtensions
    {
        public static XElement ToXElement<T>(this XmlSerializer xmlSerializer, T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    xmlSerializer.Serialize(streamWriter, obj);
                    return XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray()));
                }
            }
        }

        public static Guid GuidValue(this XElement xElement)
        {
            return Guid.Parse(xElement.Value);
        }

        public static T FromXElement<T>(this XElement xElement, XmlSerializer serializer = null)
        {
            serializer = serializer ?? new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(xElement.CreateReader());
        }

        public static Boolean ValidateAgainstSchemaString(this XElement xElement, String schemaString)
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(schemaString)));
            var document = new XDocument(xElement);
            var validationHasPassed = true;
            document.Validate(schemas, (sender, args) =>
            {
                validationHasPassed = false;
            });
            return validationHasPassed;
        }
    }
}