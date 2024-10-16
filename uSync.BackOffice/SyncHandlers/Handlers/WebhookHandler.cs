using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  handler for webhook events. 
/// </summary>
[SyncHandler(uSyncConstants.Handlers.WebhookHandler, "Webhooks", "Webhooks",
	uSyncConstants.Priorites.Webhooks, 
	Icon = "icon-webhook", 
	EntityType = UdiEntityType.Webhook, 
	IsTwoPass = false	
)]
public class WebhookHandler : SyncHandlerRoot<IWebhook, IWebhook>, ISyncHandler,
	INotificationHandler<SavedNotification<IWebhook>>,
	INotificationHandler<DeletedNotification<IWebhook>>,
	INotificationHandler<SavingNotification<IWebhook>>,
	INotificationHandler<DeletingNotification<IWebhook>>
{
	private readonly IWebhookService _webhookService;

	/// <summary>
	///  constructor
	/// </summary>
	public WebhookHandler(
		ILogger<SyncHandlerRoot<IWebhook, IWebhook>> logger,
		AppCaches appCaches,
		IShortStringHelper shortStringHelper,
		SyncFileService syncFileService,
		uSyncEventService mutexService,
		uSyncConfigService uSyncConfig,
		ISyncItemFactory itemFactory,
		IWebhookService webhookService)
		: base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
	{
		_webhookService = webhookService;
	}

	/// <inheritdoc/>
	protected override IEnumerable<uSyncAction> DeleteMissingItems(IWebhook parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
	{
		return [];
	}

	protected override async Task<IEnumerable<IWebhook>> GetChildItemsAsync(IWebhook? parent)
	{
		if (parent is null)
			return [];

		return (await _webhookService.GetAllAsync(0, 1000)).Items;
    }

    protected override IEnumerable<IWebhook> GetFolders(IWebhook? parent)
		=> GetFoldersAsync(parent).Result;

    protected override Task<IEnumerable<IWebhook>> GetFoldersAsync(IWebhook? parent)
		=> Task.FromResult(Enumerable.Empty<IWebhook>());

    /// <inheritdoc/>
    protected override IWebhook? GetFromService(IWebhook? item)
		=> item is null ? null : _webhookService.GetAsync(item.Key).Result;

	/// <inheritdoc/>
	protected override string GetItemName(IWebhook item) => item.Key.ToString();
}
