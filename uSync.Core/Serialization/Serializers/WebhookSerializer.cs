using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

/// <summary>
///  serializing webhook events
/// </summary>
[SyncSerializer("ED18C89D-A9FF-4217-9F8E-6898CA63ED81", "Webhook Serializer", uSyncConstants.Serialization.Webhook, IsTwoPass = false)]
public class WebhookSerializer : SyncSerializerBase<IWebhook>, ISyncSerializer<IWebhook>
{
	private readonly IWebhookService _webhookService;

	public WebhookSerializer(
		IEntityService entityService,
		ILogger<SyncSerializerBase<IWebhook>> logger,
		IWebhookService webhookService) 
		: base(entityService, logger)
	{
		_webhookService = webhookService;
	}

    /// <inheritdoc/>
    public override async Task DeleteItemAsync(IWebhook item)
		=> await _webhookService.DeleteAsync(item.Key);

    /// <inheritdoc/>
    public override async Task<IWebhook?> FindItemAsync(Guid key)
		=> await _webhookService.GetAsync(key);

    public override async Task<IWebhook?> FindItemAsync(string alias)
    {
		if (Guid.TryParse(alias, out Guid key))
			return await FindItemAsync(key);

		return null;
	}

	/// <inheritdoc/>
	public override string ItemAlias(IWebhook item)
		=> item.Key.ToString();

    /// <inheritdoc/>
    /// 
    public override async Task SaveItemAsync(IWebhook item)   
		=> _ = 	item.HasIdentity? await _webhookService.UpdateAsync(item) : await _webhookService.CreateAsync(item);

    /// <inheritdoc/>
    protected override async Task<SyncAttempt<IWebhook>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{ 
		var key = node.GetKey();
		var alias = node.GetAlias();

		var details = new List<uSyncChange>();

		var item = await FindItemAsync(key);	
		if (item == null)
		{
			// try and find by url/etc???
		}

		var url = node.Element("Url").ValueOrDefault(string.Empty);

		if (item == null)
		{
			item = new Webhook(url);
		}
		
		if (item.Key != key)
		{
			details.AddUpdate("Key", item.Key, key);
			item.Key = key;
		}

		if (item.Url != url)
		{
			details.AddUpdate("Url", item.Url, url);
			item.Url = url;
		}

		details.AddRange(DeserializeContentKeys(item, node));
		details.AddRange(DeserializeEvents(item, node));
		details.AddRange(DeserializeHeaders(item, node));

		return SyncAttempt<IWebhook>.Succeed(node.GetAlias(), item, ChangeType.Import, details);
	}
	
	private static List<uSyncChange> DeserializeContentKeys(IWebhook item, XElement node)
	{
		var details = new List<uSyncChange>();

		var keys = node.Element("ContentTypeKeys");
		if (keys == null) return details;

		List<Guid> newKeys = [];

		foreach (var key in keys.Elements("Key"))
		{
			var keyValue = key.ValueOrDefault(Guid.Empty);
			if (keyValue == Guid.Empty) continue;
			newKeys.Add(keyValue);
		}

		var newOrderedKeys = newKeys.Order().ToArray();
		var existingOrderedKeys = item.ContentTypeKeys.Order().ToArray();

		if (existingOrderedKeys.Equals(newOrderedKeys) is false)
		{
			details.AddUpdate("ContentTypeKeys", 
				string.Join(",", existingOrderedKeys),
				string.Join(",", newOrderedKeys)
				, "/");
			item.ContentTypeKeys = newOrderedKeys;
		}

		return details;

	}

	private static List<uSyncChange> DeserializeEvents(IWebhook item, XElement node)
	{
		var details = new List<uSyncChange>();

		var keys = node.Element("Events");
		if (keys == null) return details;

		List<string> newKeys = [];

		foreach (var eventNode in keys.Elements("Event"))
		{
			var eventValue = eventNode.ValueOrDefault(string.Empty);
			if (eventValue == string.Empty) continue;
			newKeys.Add(eventValue);
		}

		var newOrderedEvents = newKeys.Order().ToArray();
		var existingOrderedEvents = item.Events.Order().ToArray();

		if (existingOrderedEvents.Equals(newOrderedEvents) is false)
		{
			details.AddUpdate("Events", 
				string.Join(",", existingOrderedEvents),
				string.Join(",", newOrderedEvents)
				, "/");
			item.Events = newOrderedEvents;
		}

		return details;
	}

	private static List<uSyncChange> DeserializeHeaders(IWebhook item, XElement node)
	{
		var details = new List<uSyncChange>();

		var keys = node.Element("Headers");
		if (keys == null) return details;

		Dictionary<string, string> newHeaders = new();

		foreach (var header in keys.Elements("Header"))
		{
			var headerKey = header.Attribute("Key").ValueOrDefault(string.Empty);
			var headerValue = header.ValueOrDefault(string.Empty);

			if (headerKey == string.Empty) continue;
			if (newHeaders.ContainsKey(headerKey)) continue; // stop duplicates.
			newHeaders.Add(headerKey, headerValue);
		}

		var existingOrderedEvents = item.Headers.OrderBy(x => x.Key).ToDictionary();
		var newOrderedHeaders = newHeaders.OrderBy(x => x.Key).ToDictionary();

		if (existingOrderedEvents.Equals(newOrderedHeaders) is false)
		{
			details.AddUpdate("Events", 
				string.Join(",", existingOrderedEvents),
				string.Join(",", newOrderedHeaders)
				, "/");
			item.Headers = newOrderedHeaders;
		}

		return details;
	}

    protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(IWebhook item, SyncSerializerOptions options)
	{
		return uSyncTaskHelper.FromResultOf(() =>
		{

			var node = InitializeBaseNode(item, item.Url);

			node.Add(new XElement("Url", item.Url));
			node.Add(new XElement("Enabled", item.Enabled));

			node.Add(SerializeContentKeys(item));
			node.Add(SerializeEvents(item));
			node.Add(SerializeHeaders(item));

			return SyncAttempt<XElement>.Succeed(item.Url, node, typeof(IWebhook), ChangeType.Export);
		});		
	}

	private static XElement SerializeContentKeys(IWebhook item)
	{
		var keysNode = new XElement("ContentTypeKeys");
		foreach (var contentTypeKey in item.ContentTypeKeys.Order())
		{
			keysNode.Add(new XElement("Key", contentTypeKey));
		}

		return keysNode;
	}

	private static XElement SerializeEvents(IWebhook item)
	{
		var eventsNode = new XElement("Events");
		foreach(var eventItem in item.Events.Order())
		{
			eventsNode.Add(new XElement("Event", eventItem));
		}
		return eventsNode;
	}

	private static XElement SerializeHeaders(IWebhook item)
	{
		var headerNode = new XElement("Headers");
		foreach(var headerItem in item.Headers.OrderBy(x => x.Key))
		{
			headerNode.Add(new XElement("Header",
				 new XAttribute("Key", headerItem.Key),
				 new XCData(headerItem.Value)));
		}

		return headerNode;
	}
}
