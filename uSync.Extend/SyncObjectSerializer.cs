using Microsoft.Extensions.Logging;

using System.Runtime.Serialization;
using System.Xml.Linq;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Extend;

/// <summary>
///  a generic seriazlier that will serialize any object, 
/// </summary>
public abstract class SyncObjectSerializer<TObject> : SyncSerializerRoot<TObject>, ISyncSerializer<TObject>
{
    protected SyncObjectSerializer(ILogger<SyncSerializerRoot<TObject>> logger) : base(logger)
    { }

    public abstract TObject CreateItem(XElement node);

    protected virtual void SetKey(TObject item, Guid key)
    {
        var property = typeof(TObject).GetProperty("Key");
        if (property != null && property.CanWrite)
        {
            property.SetValue(item, key);
        }
    }

    protected virtual void SetAlias(TObject item, string alias)
    {
        var property = typeof(TObject).GetProperty("Alias");
        if (property != null && property.CanWrite)
        {
            property.SetValue(item, alias);
        }
    }

    protected override async Task<SyncAttempt<TObject>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var item = (await FindItemAsync(node)) ?? CreateItem(node);

        SetKey(item, node.GetKey());
        SetAlias(item, node.GetAlias());

        var propertyNode = node.Element("Properties");
        if (propertyNode != null)
        {
            var properties = typeof(TObject).GetProperties();
            foreach (var property in properties)
            {
                var valueNode = propertyNode.Element(property.Name);
                if (valueNode != null)
                {
                    var currentValue = property.GetValue(item)?.ToString() ?? string.Empty;
                    if (valueNode.Value != currentValue)
                    {
                        var value = Convert.ChangeType(valueNode.Value, property.PropertyType);
                        property.SetValue(item, value);
                    }
                }
            }
        }

        return SyncAttempt<TObject>.Succeed(ItemAlias(item), item, ChangeType.Import, []);
    }

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(TObject item, SyncSerializerOptions options)
    {
        if (item == null)
            return Task.FromResult(SyncAttempt<XElement>.Fail(string.Empty, null, ChangeType.Fail, "Item is null", new ArgumentNullException(nameof(item))));

        var node = new XElement(ItemType,
            new XAttribute(uSyncConstants.Xml.Key, ItemKey(item)),
            new XAttribute(uSyncConstants.Xml.Alias, ItemAlias(item)));

        var propertyNode = new XElement("Properties");

        Type objectType = item.GetType();
        var properies = objectType.GetProperties();
        foreach (var property in properies)
        {
            // ignore if the object has "IgnoreDataMember" attribute
            if (property.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length > 0)
                continue;

            var value = property.GetValue(item)?.ToString() ?? string.Empty;
            propertyNode.Add(new XElement(property.Name, value));
        }

        node.Add(propertyNode);

        return Task.FromResult(SyncAttempt<XElement>.Succeed(ItemAlias(item), node, ChangeType.Export, []));
    }
}
