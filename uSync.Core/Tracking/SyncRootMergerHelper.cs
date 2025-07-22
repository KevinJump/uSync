using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;


namespace uSync.Core.Tracking;

public class SyncRootMergerHelper
{
    public static XElement GetDifferences(List<XElement> nodes, IList<TrackingItem> trackedNodes)
    {
        var (_, differences) = CompareNodes(nodes, trackedNodes);
        return differences;
    }

    public static XElement GetCombined(List<XElement> nodes, IList<TrackingItem> trackedNodes)
    {
        var (combined, _) = CompareNodes(nodes, trackedNodes);
        return combined;
    }

    public static XElement GetDifferencesByFileContents(List<XElement> nodes)
	{
        /// work out what is the 'latest' version of the node we are using for comparison.
        /// 
        // Node1, Node2, Node3 
        if (nodes is null || nodes.Count == 0) return null;

		var last = nodes[0]; // Node1
		bool hasDifference = false;
		for (int n = 1; n < nodes.Count; n++)
		{
			// first pass Node2
			// second pass Node3
			if (GetDifferenceByContents(last, nodes[n]) != null)
			{
				last = nodes[n]; // last = node2. 
				hasDifference = true;
			}
			{
				// if Node1 and Node2 are the same. 
				// last == node1
			}
		}
		// at the end last will either still be node1, or node2 or node3. 
		if (hasDifference is false) return null;
		return last;
	}

	public static XElement GetDifferenceByContents(XElement source, XElement target)
	{
		// if the two files are identical there are no changes. 
		if (source.MakePlatformSafeHash() != target.MakePlatformSafeHash()) return null;
		return target;
	}


	public static (XElement combined, XElement differences) CompareNodes(List<XElement> nodes, IList<TrackingItem> trackedNodes)
    {
        var differences = XElement.Parse(nodes[^1].ToString());
        var combined = XElement.Parse(nodes[^1].ToString());

        // latest is a blank one, that is a delete so should 
        // be marked as one.
        if (combined.IsEmptyItem())
            return (combined, BlankNode(differences));
            
        foreach (var node in nodes[..^1])
        {
            // if this node is the same as the differences we already have, 
            // return it. 
            if (node.MakePlatformSafeHash() == differences.MakePlatformSafeHash())
                return (combined, BlankNode(differences));

            // workout any merged differences, 
            (combined, differences) = GetTrackedNodeDifferences(node, combined, trackedNodes);
        }
        return (combined, differences);
    }

    private static (XElement combined, XElement differences) GetTrackedNodeDifferences(XElement source, XElement target, IList<TrackingItem> trackedNodes)
    {
        if (source.ToString() == target.ToString())
            return (source, BlankNode(target));

        var (combined, differences) = GetChanges(source, target, trackedNodes);

        return (combined, differences);
    }

    private static (XElement combined, XElement diffrence) GetChanges(XElement source, XElement target, IList<TrackingItem> items)
    {
        foreach (var item in items)
        {
            XElement node;

            if (item.SingleItem is false)
            {
                var path = item.Path.Contains('*')
                    ? item.Path.Substring(0, item.Path.IndexOf('*')).TrimEnd('/')
                    : item.Path.Substring(0, item.Path.LastIndexOf('/'));

                var (combined, difference) = GetMultipleChanges(item, source, target);
                if (difference != null)
                {
                    var replacementNode = target.XPathSelectElement(path);
                    if (difference.HasElements)
                    {
                        replacementNode?.AddAfterSelf(difference);
                    }

                    replacementNode?.Remove();
                }

                if (combined != null && (combined.HasElements || combined.HasAttributes))
                {
                    var replacementNode = source.XPathSelectElement(path);
                    replacementNode?.AddAfterSelf(combined);
                    replacementNode?.Remove();
                }
            }
            else
            {
                node = GetSingleChange(item, source, target);
                if (node == null)
                {
                    target.XPathSelectElement(item.Path)?.Remove();
                }
                else if (node.Name.LocalName != "deleted")
                {
                    var add = source.XPathSelectElement(item.Path);
                    add?.AddAfterSelf(node);
                    add?.Remove();
                }
            }
        }

        return (source, target);
    }

    private static XElement GetSingleChange(TrackingItem item, XElement source, XElement target)
    {
        var sourceNode = source.XPathSelectElement(item.Path);
        var targetNode = target.XPathSelectElement(item.Path);

        if (sourceNode == null) return targetNode;
        if (targetNode == null)
        {
            // item has been removed from target. 
            return new XElement("deleted");
        }

        // they match 
        if (sourceNode.ToString() == targetNode.ToString())
            return null;

        return targetNode;
    }

    private static (XElement combined, XElement diffrence) GetMultipleChanges(TrackingItem item, XElement source, XElement target)
    {
        if (item.Path.Contains('*')) 
            return GetWildcardChanges(item, source, target);

        var path = item.Path.Substring(0, item.Path.LastIndexOf('/'));
        var element = item.Path.Substring(item.Path.LastIndexOf('/')+1);

        var sourceCollection = source.XPathSelectElement(path);
        var targetCollection = target.XPathSelectElement(path);

        if (targetCollection == null || sourceCollection == null)
            return (sourceCollection, targetCollection);

        var differenceCollection = XElement.Parse(targetCollection.ToString());
        var combinedCollection = XElement.Parse(sourceCollection.ToString());

        foreach (var sourceElement in sourceCollection.Elements(element))
        {
            var key = GetKey(sourceElement, item.Keys);
            var targetElement = FindByKey(targetCollection, element, item.Keys, key);

            if (targetElement == null)
            {
                differenceCollection.Add(MakeDeletedElement(element, item.Keys, key));
                // FindByKey(combinedCollection, element, item.Keys, key)?.Remove();
                continue;
            }

            if (sourceElement.ToString() == targetElement.ToString())
            {
                var removal = FindByKey(differenceCollection, element, item.Keys, key);
                removal.Remove();
            }
            else
            {
                var replacement = FindByKey(combinedCollection, element, item.Keys, key);

                // only add this if its not a delete
                if (targetElement.Attribute("deleted").ValueOrDefault(false) is false)
                {
                    replacement?.AddAfterSelf(targetElement);
                }
                replacement?.Remove();
            }
        }

        foreach (var targetElement in targetCollection.Elements(element))
        {
            var key = GetKey(targetElement, item.Keys);
            var sourceElement = FindByKey(sourceCollection, element, item.Keys, key);

            if (sourceElement == null)
                combinedCollection.Add(targetElement);
        }

        if (!string.IsNullOrWhiteSpace(item.SortingKey))
        {
            combinedCollection = SortElement(combinedCollection, element, item.SortingKey);
        }

        return (combinedCollection, differenceCollection);
    }

    /// <summary>
    ///  changes where the path contains a wildcard (e.g /item/*/value)
    /// </summary>
    private static (XElement combined, XElement diffrences) GetWildcardChanges(TrackingItem item, XElement source, XElement target)
    {
        var rootPath = item.Path.Substring(0, item.Path.IndexOf("/*"));

        var path = item.Path.Substring(0, item.Path.LastIndexOf('/'));
        var element = item.Path.Substring(item.Path.LastIndexOf('/') + 1);

        var sourceCollection = source.XPathSelectElements(path);
        var targetCollection = target.XPathSelectElements(path);

        if (sourceCollection == null || targetCollection == null)
            return (source, target);

        var combined = XElement.Parse(source.ToString());
        var differences = XElement.Parse(target.ToString());

        foreach (var sourceElement in sourceCollection)
        {
            var elementPath = item.Path.Replace("*", sourceElement.Name.LocalName);

            var sourceNode = combined.XPathSelectElement(elementPath);
            var targetNode = differences.XPathSelectElement(elementPath);

            if (sourceNode == null || targetNode == null) continue;

            if (targetNode.Attribute("deleted")?.Value == "true")
            {
                // if the target node is deleted, we need to remove it from the source
                combined.XPathSelectElement(elementPath)?.Remove();
                continue;
            }

            // compare. 
            var sourceValue = sourceNode.ValueOrDefault(string.Empty);
            var targetValue = targetNode.ValueOrDefault(string.Empty);

            if (sourceValue.Equals(targetValue) is true)
            {
                // the same, remove from differences
                differences.XPathSelectElement(elementPath)?.Remove();
            }
            else
            {
                var replacement = combined.XPathSelectElement(elementPath);
                replacement?.AddAfterSelf(targetNode);
                replacement?.Remove();
            }
        }

        return (
            combined.XPathSelectElement(rootPath),
            RemoveEmptyChildren(differences.XPathSelectElement(rootPath))
        ); 
    }

    private static XElement SortElement(XElement node, string elementName, string key)
    {
        var keyName = key.StartsWith('#') ? key.Substring(1) : key;

        List<XElement> sorted = key.StartsWith('#')
            ?  string.IsNullOrWhiteSpace(keyName) 
                ? [.. node.Elements(elementName).OrderBy(e => e.Value ?? "")]
                : [.. node.Elements(elementName).OrderBy(e => e.Element(keyName)?.Value ?? "")]
            : [.. node.Elements(elementName).OrderBy(x => (string)x.Element(key) ?? "")];

        node.RemoveNodes();
        node.Add(sorted);
        return node;
    }

    private static string GetKey(XElement collection, string keyName)
    {
        if (keyName.StartsWith('@'))
        {
            return collection.Attribute(keyName.Substring(1))?.Value ?? string.Empty; ;
        }
        return collection.Element(keyName)?.Value ?? string.Empty;
    }

    private static XElement FindByKey(XElement collection, string element, string keyName, string keyValue)
        => collection.XPathSelectElement($"{element}[{keyName} = {EscapeXPathString(keyValue)}]");

    private static XElement MakeDeletedElement(string element, string keyName, string key)
    {
        var deletedElement = new XElement(element,
            new XAttribute("deleted", true));

        if (keyName.StartsWith('@'))
            deletedElement.Add(new XAttribute(keyName.Substring(1), key));
        else
            deletedElement.Add(new XElement(keyName, key));

        return deletedElement;
    }

    private static string EscapeXPathString(string value)
    {
        if (!value.Contains('\''))
            return '\'' + value + '\'';

        if (!value.Contains('"'))
            return '"' + value + '"';

        return "concat('" + value.Replace("'", "',\"'\",'") + "')";
    }

    private static XElement BlankNode(XElement source)
    {
        var blank = XElement.Parse(source.ToString());
        blank.RemoveNodes();
        return blank;
    }

    /// <summary>
    /// Removes all empty child elements from the given XElement node recursively.
    /// An element is considered empty if it has no child elements, no attributes, and no value.
    /// </summary>
    public static XElement RemoveEmptyChildren(XElement node)
    {
        if (node == null) return node;

        // Recursively process child nodes to check they don't have any empty children
        foreach (var child in node.Elements().Where(e => e.HasElements || e.HasAttributes))
        {
            RemoveEmptyChildren(child);
        }

        // Make a list to avoid modifying the collection while iterating
        var emptyChildren = node.Elements()
            .Where(e => !e.HasElements && string.IsNullOrWhiteSpace(e.Value) && !e.HasAttributes)
            .ToList();
        foreach (var empty in emptyChildren)
        {
            empty.Remove();
        }

        return node;
    }
}
