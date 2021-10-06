using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;

namespace uSync8.Core.Extensions
{
    public static class PropertyGroupExtensions
    {
        /// <summary>
        ///  does this version of umbraco support tabs?
        /// </summary>
        /// <remarks>
        ///  reflection is fast, but a version check is faster, so we wrap things where we
        ///  don't have to do the reflection, 
        /// </remarks>
        public static bool SupportsTabs
            => UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 17;

        public static PropertyGroup FindTab(this PropertyGroupCollection groups, string nameOrAlias)
        {
            var index = groups.IndexOfKey(nameOrAlias);
            if (index != -1)
                return groups[index];

            return null;
        }

        /// <summary>
        ///  Get a property from the tab and return it as a string. 
        /// </summary>
        /// <remarks>
        ///  useing reflection to get properties that may not have existed in earlier versions
        /// </remarks>
        public static string GetTabPropertyAsString(this PropertyGroup tab, string propertyName)
        {
            if (SupportsTabs)
            {
                var propertyInstance = typeof(PropertyGroup).GetProperty(propertyName);
                if (propertyInstance != null)
                {
                    var propertyValue = propertyInstance.GetValue(tab);
                    var attempt = propertyValue.TryConvertTo<string>();
                    if (attempt.Success) return attempt.Result;
                }
            }

            return string.Empty;
        }

        /// <summary>
        ///  Set the tab type through relection
        /// </summary>
        public static void SetGroupType(this PropertyGroup group, string type)
        {
            if (SupportsTabs)
            {
                // get the tab type through reflection.
                var tabType = typeof(PropertyGroup).GetProperty("Type");
                if (tabType != null)
                {
                    var tabTypeEnum = tabType.PropertyType.GetField(type);
                    if (tabTypeEnum != null)
                    {
                        tabType.SetValue(group, tabTypeEnum.GetValue(tabType));
                    }
                }
            }
        }

        /// <summary>
        ///  safely add a property group - using either the 8.17+ method or the legacy method.
        /// </summary>
        /// <remarks>
        ///  n.b. we are using reflection so we can continue to support previous versions of Umbraco.
        /// </remarks>
        public static void SafeAddPropertyGroup(this IContentTypeComposition item, string alias, string name)
        {
            if (SupportsTabs)
            {
                var addPropertyGroupMethod = typeof(IContentTypeComposition).GetMethod("AddPropertyGroup",
                    new Type[] { typeof(string), typeof(string) });

                if (addPropertyGroupMethod != null)
                {
                    addPropertyGroupMethod.Invoke(item, new[] { alias, name });
                    return;
                }
            }

            item.AddPropertyGroup(name);
        }

        /// <summary>
        ///  safely add a property type (to the right tab location). 
        /// </summary>
        /// <remarks>
        ///  uses reflection because the methods have changed in later versions of Umbraco and we want to 
        ///  maintain some backwards compatability.
        /// </remarks>
        public static void SafeAddPropertyType(this IContentTypeComposition item, PropertyType property, string alias, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                item.AddPropertyType(property);
            }
            else
            {
                if (SupportsTabs)
                {
                    var addPropertyTypeMethod = typeof(IContentTypeComposition).GetMethod("AddPropertyType",
                        new Type[] { typeof(PropertyType), typeof(string), typeof(string) });

                    if (addPropertyTypeMethod != null)
                    {
                        addPropertyTypeMethod.Invoke(item, new object[] { property, alias, name });
                        return;
                    }
                }

                item.AddPropertyType(property, name);
            }
        }

        /// <summary>
        ///  get the tab alias or name from an existing tab.
        /// </summary>
        public static string GetTabAliasOrName(this PropertyGroup tab)
        {
            var alias = tab.GetTabPropertyAsString("Alias");
            if (!string.IsNullOrWhiteSpace(alias)) return alias;

            return tab.Name;
        }


        /// <summary>
        ///  Get the tab alias or name for an item we are importing. 
        /// </summary>
        public static string GetTabAliasOrName(this PropertyGroupCollection groups, XElement tabNode)
        {
            if (tabNode == null) return string.Empty;

            var alias = tabNode.Attribute("Alias").ValueOrDefault(string.Empty);
            if (!string.IsNullOrWhiteSpace(alias)) return alias;

            var name = tabNode.ValueOrDefault(string.Empty);
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            // if we only have the name, lookup the tab and return the alias 
            var tab = groups.FindTab(name);
            if (tab != null) return tab.GetTabAliasOrName();

            // if all else fails we return the alias to the name, 
            // this will create a group with this name - which is the default behavior.
            return name;

        }

        /// <summary>
        ///  Calculates what the default type should be when adding tab/groups to this content type.
        /// </summary>
        public static string GetDefaultTabType(this XElement node)
        {
            // if we have tabs then when we don't know the type of a group, its a tab.
            if (node.Elements("Tab").Any(x => x.Element("Type").ValueOrDefault("Group") == "Tab"))
                return "Tab";

            // if we don't have tabs then all unknown types are groups.
            return "Group";
        }


    }
}
