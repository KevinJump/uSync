using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

using Serilog.Core;

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

            // workout any merged diffrences, 
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
                var path = item.Path.Substring(0, item.Path.LastIndexOf('/'));

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

                if (combined != null)
                {
                    var replacementNode = source.XPathSelectElement(path);
                    if (combined.HasElements)
                    {
                        replacementNode?.AddAfterSelf(combined);
                        replacementNode?.Remove();
                    }
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
        var path = item.Path.Substring(0, item.Path.LastIndexOf('/'));
        var element = item.Path.Substring(item.Path.LastIndexOf("/")+1);

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

    private static XElement SortElement(XElement node, string elementName, string key)
    {
        var sorted = node.Elements(elementName)
            .OrderBy(x => (string)x.Element(key) ?? "")
            .ToList();

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
}
