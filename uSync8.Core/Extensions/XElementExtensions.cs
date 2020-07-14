using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Extensions
{
    public static class XElementExtensions
    {
        public static int GetLevel(this XElement node)
            => node.Attribute("Level").ValueOrDefault(0);

        public static Guid GetKey(this XElement node)
            => node.Attribute("Key").ValueOrDefault(Guid.Empty);

        public static string GetAlias(this XElement node)
            => node.Attribute("Alias").ValueOrDefault(string.Empty);

        public static string GetCultures(this XElement node)
            => node.Attribute(uSyncConstants.CultureKey).ValueOrDefault(string.Empty);

        public static string GetSegments(this XElement node)
            => node.Attribute(uSyncConstants.SegmentKey).ValueOrDefault(string.Empty);

        /// <summary>
        ///  Get the key of any parent value that is in the file.
        /// </summary>
        /// <remarks>
        ///  Not all items have a parent
        /// </remarks>
        public static Guid GetParentKey(this XElement node)
        {
            var result = node
                .Element("Info")?
                .Element("Parent")?
                .Attribute("Key").ValueOrDefault(Guid.Empty);

            return result != null && result.HasValue
                ? result.Value 
                : Guid.Empty;
        }

        public static string ValueOrDefault(this XElement node, string defaultValue)
        {
            if (node == null || string.IsNullOrEmpty(node.Value))
                return defaultValue;

            return node.Value;
        }

        public static bool IsEmptyItem(this XElement node)
        {
            return node.Name.LocalName == uSyncConstants.Serialization.Empty;
        }

        /// <summary>
        ///  makes an uSync empty file
        /// </summary>
        /// <remarks>
        ///  When the changetype is Clean, the key should be the parent folder 
        ///  you want to clean. 
        /// </remarks>
        /// <typeparam name="TObject">type of object (IEntity based)</typeparam>
        public static XElement MakeEmpty(Guid key, SyncActionType change, string alias)
        {
            return new XElement(uSyncConstants.Serialization.Empty,
                new XAttribute("Key", key),
                new XAttribute("Alias", alias),
                new XAttribute("Change", change));
        }

        public static SyncActionType GetEmptyAction(this XElement node)
        {
            if (IsEmptyItem(node))
                return node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);

            return SyncActionType.None;
        }

        public static TObject ValueOrDefault<TObject>(this XElement node, TObject defaultValue)
        {
            var value = ValueOrDefault(node, string.Empty);
            if (value == string.Empty) return defaultValue;

            var attempt = value.TryConvertTo<TObject>();
            if (attempt)
                return attempt.Result;

            return defaultValue;
        }

        public static XElement FindOrCreate(this XElement node, string name)
        {
            if (node == null) return null;

            var element = node.Element(name);
            if (element == null)
            {
                element = new XElement(name);
                node.Add(element);
            }
            return element;
        }

        public static XElement FindOrCreate(this XElement node, string name, string attributeName, string value)
        {
            var elements = node.Elements(name);
            if (elements != null)
            {
                var foundElement = elements
                    .Where(x => x.Attribute(attributeName)
                    .ValueOrDefault(string.Empty).InvariantEquals(value))
                    .FirstOrDefault();

                if (foundElement != null) return foundElement;
            }

            // else 
            var element = new XElement(name,
                new XAttribute(attributeName, value));
            node.Add(element);

            return element;   
        }

        public static void CreateOrSetElement(this XElement node, string name, string value)
        {
            if (node == null) return;

            var element = node.Element(name);
            if (element == null)
            {
                element = new XElement(name);
                node.Add(element);
            }
            element.Value = value;
        }

        public static void CreateOrSetElement<TObject>(this XElement node, string name, TObject value)
        {
            if (node == null) return;

            var attempt = value.TryConvertTo<string>();
            if (attempt.Success)
            {
                var element = node.Element(name);
                if (element == null)
                {
                    element = new XElement(name);
                    node.Add(element);
                }

                element.Value = attempt.Result;
            }
        }

        /// <summary>
        ///  strips any missing attribute based values from the element list if they are not in the keys list.
        /// </summary>
        public static void RemoveMissingElements(this XElement node, string elements, string keyName, IEnumerable<string> keys)
        {
            var stripped = new XElement(node.Name.LocalName);
            bool changed = false;

            foreach(var element in node.Elements(elements))
            {
                var key = element.Attribute(keyName).ValueOrDefault(string.Empty);
                if (keys.Contains(key))
                {
                    stripped.Add(element);
                }
                else
                {
                    changed = true;
                }
            }

            if (changed)
            {
                node.Parent.Add(stripped);
                node.Remove();
            }
        }
             
        /// <summary>
        ///  gets a value from an element, if its is missing throws an ArgumentNullException
        /// </summary>
        public static string RequiredElement(this XElement node, string name)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var val = node.Element(name).ValueOrDefault(string.Empty);
            if (val == string.Empty)
                throw new ArgumentNullException("Missing Value " + name);

            return val;
        }

        #region Attribute Extensions

        /// <summary>
        ///  Get the Value from the attribute or return the default value if attribute is not set
        /// </summary>
        public static string ValueOrDefault(this XAttribute attribute, string defaultValue)
        {
            if (attribute == null || string.IsNullOrEmpty(attribute.Value))
                return defaultValue;

            return attribute.Value;
        }

        /// <summary>
        ///  Get the Value from the attribute or return the default value if attribute is not set
        /// </summary>
        public static TObject ValueOrDefault<TObject>(this XAttribute attribute, TObject defaultValue)
        {
            var value = attribute.ValueOrDefault(string.Empty);
            if (value == string.Empty) return defaultValue;

            var attempt = value.TryConvertTo<TObject>();
            if (attempt)
                return attempt.Result;

            return defaultValue;
        }
        #endregion
    }
}
