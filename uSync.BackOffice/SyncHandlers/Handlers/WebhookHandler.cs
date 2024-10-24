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
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
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
	INotificationAsyncHandler<SavedNotification<IWebhook>>,
	INotificationAsyncHandler<DeletedNotification<IWebhook>>,
	INotificationAsyncHandler<SavingNotification<IWebhook>>,
	INotificationAsyncHandler<DeletingNotification<IWebhook>>
{
	private readonly IWebhookService _webhookService;

	/// <summary>
	///  constructor
	/// </summary>
	public WebhookHandler(
		ILogger<SyncHandlerRoot<IWebhook, IWebhook>> logger,
		AppCaches appCaches,
		IShortStringHelper shortStringHelper,
		ISyncFileService syncFileService,
		ISyncEventService mutexService,
		uSyncConfigService uSyncConfig,
		ISyncItemFactory itemFactory,
		IWebhookService webhookService)
		: base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
	{
		_webhookService = webhookService;
	}

    /// <inheritdoc/>
    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(IWebhook parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
		=> Task.FromResult(Enumerable.Empty<uSyncAction>());

    protected override async Task<IEnumerable<IWebhook>> GetChildItemsAsync(IWebhook? parent)
		=> parent is not null ? [] : (await _webhookService.GetAllAsync(0, 1000)).Items;

    protected override Task<IEnumerable<IWebhook>> GetFoldersAsync(IWebhook? parent)
		=> Task.FromResult(Enumerable.Empty<IWebhook>());

    /// <inheritdoc/>
	protected override async Task<IWebhook?> GetFromServiceAsync(IWebhook? item)
		=> item is null ? null : await _webhookService.GetAsync(item.Key);

	/// <inheritdoc/>
	protected override string GetItemName(IWebhook item) => item.Key.ToString();
}
