using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

using Umbraco.Extensions;

namespace uSync.Core;

public static class XElementExtensions
{
    /// <summary>
    ///  Summary the level (in the tree) for the item represented by the xml
    /// </summary>
    public static int GetLevel(this XElement node)
        => node.Attribute(uSyncConstants.Xml.Level).ValueOrDefault(0);

    /// <summary>
    ///  the Key (guid) for the item represented by the xml
    /// </summary>
    public static Guid GetKey(this XElement node)
        => node.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

    /// <summary>
    ///  The alias for the item represented by the xml
    /// </summary>
    public static string GetAlias(this XElement node)
        => node.Attribute(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty);

    public static int GetItemSortOrder(this XElement node)
        => node.Element(uSyncConstants.Xml.Info)?
            .Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(0) ?? 0;

    /// <summary>
    ///  cultures contained within the xml
    /// </summary>
    public static string GetCultures(this XElement node)
        => node.Attribute(uSyncConstants.CultureKey).ValueOrDefault(string.Empty);

    /// <summary>
    ///  Segments contained within the xml
    /// </summary>
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
            .Element(uSyncConstants.Xml.Info)?
            .Element(uSyncConstants.Xml.Parent)?
            .Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

        return result is not null && result.HasValue
            ? result.Value
            : Guid.Empty;
    }

    /// <summary>
    ///  get the nice path name that is stored in the xml, gives us something to show
    ///  the user in terms of location.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static string GetPath(this XElement node)
        => node.Element(uSyncConstants.Xml.Info)?
            .Element(uSyncConstants.Xml.Path).ValueOrDefault(string.Empty) ?? string.Empty;

    /// <summary>
    ///  does the xml represent an 'Empty' item (deleted/renamed/etc)
    /// </summary>
    public static bool IsEmptyItem(this XElement node)
        => node.Name.LocalName == uSyncConstants.Serialization.Empty;

    /// <summary>
    ///  makes an uSync empty file
    /// </summary>
    /// <remarks>
    ///  When the change type is Clean, the key should be the parent folder 
    ///  you want to clean. 
    /// </remarks>
    /// <typeparam name="TObject">type of object (IEntity based)</typeparam>
    public static XElement MakeEmpty(Guid key, SyncActionType change, string alias)
        => new XElement(uSyncConstants.Serialization.Empty,
            new XAttribute(uSyncConstants.Xml.Key, key),
            new XAttribute(uSyncConstants.Xml.Alias, alias),
            new XAttribute(uSyncConstants.Xml.Change, change));

    /// <summary>
    ///  return the uSyncActionType of the empty XML file.
    /// </summary>
    public static SyncActionType GetEmptyAction(this XElement node)
        => node.IsEmptyItem()
            ? node.Attribute(uSyncConstants.Xml.Change).ValueOrDefault(SyncActionType.None)
            : SyncActionType.None;

    public static string GetItemType(this XElement node)
        => node.IsEmptyItem()
            ? node.Attribute(uSyncConstants.Xml.ItemType).ValueOrDefault(string.Empty)
            : node.Name.LocalName;

    /// <summary>
    ///  helper - is the current node a content item.
    /// </summary>
    public static bool IsContent(this XElement node)
        => node.GetItemType().Equals("content", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///  is the current node for a content blueprint. 
    /// </summary>
    /// <remarks>
    ///  sometimes depending on how the import has been fired we can't tell just from the 
    ///  folder if we are importing content or a blueprint - the helper function lets 
    ///  us work this out.
    /// </remarks>
    public static bool IsBlueprint(this XElement node)
        => node.Element("Info")?.Element("IsBlueprint").ValueOrDefault(false) is true;


    /// <summary>
    ///  Get the value of the XML Node or return a default value
    /// </summary>
    public static string ValueOrDefault([AllowNull] this XElement? node, string defaultValue)
        => string.IsNullOrEmpty(node?.Value)
            ? defaultValue
            : node.Value;

    /// <summary>
    ///  Get the value of the XML Node or return a default value
    /// </summary>
    public static TObject ValueOrDefault<TObject>([AllowNull] this XElement? node, TObject defaultValue)
    {
        var value = node.ValueOrDefault(string.Empty);
        if (value == string.Empty) return defaultValue;

        var attempt = value.TryConvertTo<TObject>();
        if (attempt)
            return attempt.Result ?? defaultValue;

        return defaultValue;
    }


    /// <summary>
    ///  Find a node in the XML or create it if it doesn't exist
    /// </summary>
    public static XElement? FindOrCreate(this XElement node, string name)
    {
        if (node is null) return null;

        var element = node.Element(name);
        if (element is null)
        {
            element = new XElement(name);
            node.Add(element);
        }
        return element;
    }

    /// <summary>
    ///  Find a node in the xml by its attribute name, create it if it doesn't exist
    /// </summary>
    public static XElement FindOrCreate(this XElement node, string name, string attributeName, string value)
    {
        var elements = node.Elements(name);
        if (elements is not null)
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

    /// <summary>
    ///  set the value for an element in the xml, if it doesn't exist create it and set the value
    /// </summary>
    public static void CreateOrSetElement(this XElement node, string name, string value)
    {
        if (node is null) return;

        var element = node.Element(name);
        if (element is null)
        {
            element = new XElement(name);
            node.Add(element);
        }
        element.Value = value;
    }

    /// <summary>
    ///  set the value for an element in the xml, if it doesn't exist create it and set the value
    /// </summary>
    public static void CreateOrSetElement<TObject>(this XElement node, string name, TObject value)
    {
        if (node is null) return;

        var attempt = value.TryConvertTo<string>();
        if (attempt.Success)
        {
            var element = node.Element(name);
            if (element is null)
            {
                element = new XElement(name);
                node.Add(element);
            }

            element.Value = attempt.Result ?? string.Empty;
        }
    }

    /// <summary>
    ///  strips any missing attribute based values from the element list if they are not in the keys list.
    /// </summary>
    public static void RemoveMissingElements(this XElement node, string elements, string keyName, IEnumerable<string> keys)
    {
        var stripped = new XElement(node.Name.LocalName);
        bool changed = false;

        foreach (var element in node.Elements(elements))
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
            node.Parent?.Add(stripped);
            node.Remove();
        }
    }

    /// <summary>
    ///  gets a value from an element, if its is missing throws an ArgumentNullException
    /// </summary>
    public static string RequiredElement(this XElement node, string name)
    {
        ArgumentNullException.ThrowIfNull(node);

        var val = node.Element(name).ValueOrDefault(string.Empty);
        
        if (val == string.Empty) throw new ArgumentNullException("Missing Value " + name);

        return val;
    }

    #region Attribute Extensions

    /// <summary>
    ///  Get the Value from the attribute or return the default value if attribute is not set
    /// </summary>
    public static string ValueOrDefault([AllowNull] this XAttribute? attribute, string defaultValue)
       => string.IsNullOrEmpty(attribute?.Value) 
            ? defaultValue
            : attribute.Value;

    /// <summary>
    ///  Get the Value from the attribute or return the default value if attribute is not set
    /// </summary>
    public static TObject ValueOrDefault<TObject>([AllowNull] this XAttribute attribute, TObject defaultValue)
    {
        var value = attribute.ValueOrDefault(string.Empty);
        if (value == string.Empty) return defaultValue;

        var attempt = value.TryConvertTo<TObject>();
        if (attempt)
            return attempt.Result ?? defaultValue;

        return defaultValue;
    }
    #endregion
}
