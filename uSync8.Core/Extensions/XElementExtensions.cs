using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core;

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

        public static string ValueOrDefault(this XAttribute attribute, string defaultValue)
        {
            if (attribute == null || string.IsNullOrEmpty(attribute.Value))
                return defaultValue;

            return attribute.Value;
        }


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
