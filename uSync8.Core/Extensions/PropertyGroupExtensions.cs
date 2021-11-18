using System;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
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

        /// <summary>
        ///  prefix we put on tabs when we are appending something to them.
        /// </summary>
        public static string uSyncTmpTabAliasPrefix = "zzzusync";

        /// <summary>
        ///  get the temp alias for the tab - that will help with clashes on renames/changes of type
        /// </summary>
        public static string GetTempTabAlias(string alias)
            => $"{uSyncTmpTabAliasPrefix}{alias}";

        /// <summary>
        ///  is the current tab alias a temp alias (one we have appended zzz... to)
        /// </summary>
        public static bool IsTempTabAlias(string alias)
            => !string.IsNullOrWhiteSpace(alias) && alias.StartsWith(uSyncTmpTabAliasPrefix);

        /// <summary>
        ///  strip of any temp tab alias string from the tab name.
        /// </summary>
        public static string StripTempTabAlias(string alias)
        {
            if (IsTempTabAlias(alias))
                return alias.Substring(uSyncTmpTabAliasPrefix.Length);

            return alias;
        }

        /// <summary>
        ///  find a tab among the existing tabs.
        /// </summary>
        /// <remarks>
        ///  in v8 this is a bit painful because tabs use to only have a name, and now they 
        ///  have a name and alias, and it might be one or the other, and the case can change
        ///  so it can get complicated.
        /// </remarks>
        public static PropertyGroup FindTab(this PropertyGroupCollection groups, string nameOrAlias, string name)
        {
            // tabs - check case and don't care about case (or should we?)
            if (SupportsTabs) {
                var tab = groups.FirstOrDefault(x => x.GetTabPropertyAsString("Alias").InvariantEquals(nameOrAlias));
                if (tab != null)
                    return tab;
            }

            // 2 - check name or alias is in index
            var index = groups.IndexOfKey(nameOrAlias);
            if (index != -1)
                return groups[index];

            // 3 - down level (so we have alias, but site doesn't support tabs so we check name)
            if (!SupportsTabs && !string.IsNullOrWhiteSpace(name))
            {
                index = groups.IndexOfKey(name);
                if (index != -1)
                    return groups[index];
            }

            // 4 - tabs we might have given it a zzzname 
            if (SupportsTabs)
            {
                // check the temp alias to, because we might be in move things.
                var tempTabAlias = GetTempTabAlias(nameOrAlias);
                var tab = groups.FirstOrDefault(x => x.GetTabPropertyAsString("Alias").InvariantEquals(tempTabAlias));
                if (tab != null) 
                    return tab;
            }

            // final (does the name match anything we have?)
            var namedTab = groups.FirstOrDefault(x => x.Name.InvariantEquals(nameOrAlias));
            if (namedTab != null)
                return namedTab;

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

                return "Unknown";
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
        ///  set the alias value of a tab (if we support it).
        /// </summary>
        /// <param name="group"></param>
        /// <param name="alias"></param>
        public  static void SetGroupAlias(this PropertyGroup group, string alias)
        {
            if (SupportsTabs)
            {
                var aliasType = typeof(PropertyGroup).GetProperty("Alias");
                if (aliasType != null)
                {
                    aliasType.SetValue(group, alias);
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
                var addPropertyGroupMethod = typeof(IContentTypeBase).GetMethod("AddPropertyGroup",
                    new Type[] { typeof(string), typeof(string) });

                if (addPropertyGroupMethod != null)
                {
                    Current.Logger.Info<uSync8Core>("Adding new propery group {alias} {name} ", alias, name);
                    addPropertyGroupMethod.Invoke(item, new[] { alias, name });
                    return;
                }
                else
                {
                    Current.Logger.Warn<uSync8Core>("Umbraco is 8.17 but we can't find the add property group method");
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

                    var addPropertyTypeMethod = typeof(IContentTypeBase).GetMethod("AddPropertyType",
                        new Type[] { typeof(PropertyType), typeof(string), typeof(string) });

                    if (addPropertyTypeMethod != null)
                    {
                        Current.Logger.Info<uSync8Core>("Adding Property to group using reflection {alias} {name}", alias, name);
                        addPropertyTypeMethod.Invoke(item, new object[] { property, alias, name });
                        return;
                    }
                    else
                    {
                        Current.Logger.Warn<uSync8Core>("Version is 8.17+ but we can't find the tab property method");
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

            if (SupportsTabs)
            {
                // if we only have the name, lookup the tab and return the alias 
                var tab = groups.FindTab(name, string.Empty);
                if (tab != null) return tab.GetTabAliasOrName();
            }

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
