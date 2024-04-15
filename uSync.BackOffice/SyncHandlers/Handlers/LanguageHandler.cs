using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
	/// <summary>
	///  Handler to mange language settings in uSync
	/// </summary>
	[SyncHandler(uSyncConstants.Handlers.LanguageHandler, "Languages", "Languages", uSyncConstants.Priorites.Languages,
		Icon = "icon-globe", EntityType = UdiEntityType.Language, IsTwoPass = true)]
	public class LanguageHandler : SyncHandlerBase<ILanguage, ILocalizationService>, ISyncHandler,
		INotificationHandler<SavingNotification<ILanguage>>,
		INotificationHandler<SavedNotification<ILanguage>>,
		INotificationHandler<DeletedNotification<ILanguage>>,
		INotificationHandler<DeletingNotification<ILanguage>>
	{
		private readonly ILocalizationService localizationService;

		/// <inheritdoc/>
		public LanguageHandler(
			ILogger<LanguageHandler> logger,
			IEntityService entityService,
			ILocalizationService localizationService,
			AppCaches appCaches,
			IShortStringHelper shortStringHelper,
			SyncFileService syncFileService,
			uSyncEventService mutexService,
			uSyncConfigService uSyncConfig,
			ISyncItemFactory syncItemFactory)
			: base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
		{
			this.localizationService = localizationService;
		}

		/// <inheritdoc/>
		// language guids are not consistant (at least in alpha)
		// so we don't save by Guid we save by ISO name everytime.           
		protected override string GetPath(string folder, ILanguage item, bool GuidNames, bool isFlat)
		{
			return Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.{this.uSyncConfig.Settings.DefaultExtension}");
		}

		/// <inheritdoc/>
		protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
			=> item.IsoCode.ToSafeFileName(shortStringHelper);

		/// <summary>
		///  order the merged items, making sure the default language is first. 
		/// </summary>
		protected override IReadOnlyList<OrderedNodeInfo> GetMergedItems(string[] folders)
			=> base.GetMergedItems(folders)
				.OrderBy(x => x.Node.Element("IsDefault").ValueOrDefault(false) ? 0 : 1)
				.ToList();

		/// <summary>
		///  ensure we import the 'default' language first, so we don't get errors doing it. 
		/// </summary>
		/// <remarks>
		///  prost v13.1 this method isn't used to determain the order for all options.
		/// </remarks>
		protected override IEnumerable<string> GetImportFiles(string folder)
		{
			var files = base.GetImportFiles(folder);

			try
			{
				Dictionary<string, string> ordered = new Dictionary<string, string>();
				foreach (var file in files)
				{
					var node = XElement.Load(file);
					var order = (node.Element("IsDefault").ValueOrDefault(false) ? "0" : "1") + Path.GetFileName(file);
					ordered[file] = order;
				}

				return ordered.OrderBy(x => x.Value).Select(x => x.Key).ToList();
			}
			catch
			{
				return files;
			}

		}

		/// <inheritdoc/>
		protected override IEnumerable<IEntity> GetChildItems(int parent)
		{
			if (parent == -1)
				return localizationService.GetAllLanguages();

			return Enumerable.Empty<IEntity>();
		}

		/// <inheritdoc/>
		protected override string GetItemName(ILanguage item) => item.IsoCode;

		/// <inheritdoc/>
		protected override void CleanUp(ILanguage item, string newFile, string folder)
		{
			base.CleanUp(item, newFile, folder);

			// for languages we also clean up by id. 
			// this happens when the language changes .
			var physicalFile = syncFileService.GetAbsPath(newFile);
			var installedLanguages = localizationService.GetAllLanguages()
				.Select(x => x.IsoCode).ToList();

			var files = syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}");

			foreach (string file in files)
			{
				var node = syncFileService.LoadXElement(file);
				var IsoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);

				if (!String.IsNullOrWhiteSpace(IsoCode))
				{
					if (!file.InvariantEquals(physicalFile))
					{
						// not the file we just saved, but matching IsoCode, we remove it.
						if (node.Element("IsoCode").ValueOrDefault(string.Empty) == item.IsoCode)
						{
							logger.LogDebug("Found Matching Lang File, cleaning");
							var attempt = serializer.SerializeEmpty(item, SyncActionType.Rename, node.GetAlias());
							if (attempt.Success)
							{
								syncFileService.SaveXElement(attempt.Item, file);
							}
						}
					}

					if (!installedLanguages.InvariantContains(IsoCode))
					{
						// language is no longer installed, make the file empty. 
						logger.LogDebug("Language in file is not on the site, cleaning");
						var attempt = serializer.SerializeEmpty(item, SyncActionType.Delete, node.GetAlias());
						if (attempt.Success)
						{
							syncFileService.SaveXElement(attempt.Item, file);
						}
					}
				}
			}
		}

		private static ConcurrentDictionary<string, string> newLanguages = new ConcurrentDictionary<string, string>();

		/// <inheritdoc/>
		public override void Handle(SavingNotification<ILanguage> notification)
		{
			if (_mutexService.IsPaused) return;

			if (ShouldBlockRootChanges(notification.SavedEntities))
			{
				notification.Cancel = true;
				notification.Messages.Add(GetCancelMessageForRoots());
				return;
			}

			foreach (var item in notification.SavedEntities)
			{
				// 
				if (item.Id == 0)
				{
					newLanguages[item.IsoCode] = item.CultureName;
					// is new, we want to set this as a flag, so we don't do the full content save.n
					// newLanguages.Add(item.IsoCode);
				}
			}
		}

		/// <inheritdoc/>
		public override void Handle(SavedNotification<ILanguage> notification)
		{
			if (_mutexService.IsPaused) return;

			foreach (var item in notification.SavedEntities)
			{
				bool newItem = false;
				if (newLanguages.Count > 0 && newLanguages.ContainsKey(item.IsoCode))
				{
					newItem = true;
					newLanguages.TryRemove(item.IsoCode, out string name);
				}

				var targetFolders = GetDefaultHandlerFolders();

				if (item.WasPropertyDirty("IsDefault"))
				{
					// changing, this change doesn't trigger a save of the other languages.
					// so we need to save all language files. 
					this.ExportAll(targetFolders, DefaultConfig, null);
				}


				var attempts = Export(item, targetFolders, DefaultConfig);

				if (!newItem && item.WasPropertyDirty(nameof(ILanguage.IsoCode)))
				{
					// The language code changed, this can mean we need to do a full content export. 
					// + we should export the languages again!
					uSyncTriggers.TriggerExport(targetFolders, new List<string>() {
						UdiEntityType.Document, UdiEntityType.Language }, null);
				}

				// we always clean up languages, because of the way they are stored. 
				foreach (var attempt in attempts.Where(x => x.Success))
				{
					this.CleanUp(item, attempt.FileName, targetFolders.Last());
				}

			}
		}

		/// <summary>
		///  we don't support language deletion (because the keys are unstable)
		/// </summary>
		protected override IEnumerable<uSyncAction> DeleteMissingItems(int parentId, IEnumerable<Guid> keys, bool reportOnly)
			=> Enumerable.Empty<uSyncAction>();

	}
}
