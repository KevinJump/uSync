using System.Diagnostics;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
public class ContentSerializer : ContentSerializerBase<IContent>, ISyncSerializer<IContent>
{
    protected readonly IContentService contentService;
    protected readonly IUserService userService;

    protected readonly ITemplateService _templateService;

    public ContentSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentSerializer> logger,
        IContentService contentService,
        SyncValueMapperCollection syncMappers,
        IUserService userService,
        ITemplateService templateService)
        : base(entityService, languageService, relationService, shortStringHelper, logger, UmbracoObjectTypes.Document, syncMappers)
    {
        this.contentService = contentService;

        this.relationAlias = Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias;
        this.userService = userService;
        _templateService = templateService;
    }

    #region Serialization

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IContent item, SyncSerializerOptions options)
    {
        var node = InitializeNode(item, item.ContentType.Alias, options);

        var info = await SerializeInfoAsync(item, options);

        var properties = SerializeProperties(item, options);

        node.Add(info);
        node.Add(properties);

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Id.ToString(), node, typeof(IContent), ChangeType.Export);
    }

    protected override async Task<XElement> SerializeInfoAsync(IContent item, SyncSerializerOptions options)
    {
        var info = await base.SerializeInfoAsync(item, options);

        info.Add(SerializePublishedStatus(item, options));
        info.Add(SerializeSchedule(item, options));
        info.Add(SerializeTemplate(item, options));

        if (options.GetSetting<bool>("IncludeUserInfo", false))
        {
            info.Add(SerializerWriterInfo(item, options));
        }

        return info;
    }

    protected virtual async Task<XElement> SerializeTemplate(IContent item, SyncSerializerOptions options)
    {
        if (item.TemplateId != null && item.TemplateId.HasValue)
        {
            var template = await _templateService.GetAsync(item.TemplateId.Value);
            if (template != null)
            {
                return new XElement(uSyncConstants.Xml.Template,
                    new XAttribute(uSyncConstants.Xml.Key, template.Key),
                    template.Alias);
            }
        }
        return new XElement(uSyncConstants.Xml.Template);
    }

    private static XElement SerializePublishedStatus(IContent item, SyncSerializerOptions options)
    {
        // get the list of cultures we are serializing from the configuration
        var activeCultures = options.GetCultures();

        var published = new XElement("Published");

        // to make this a non-breaking change, we say default = item.published, but when 
        // dealing with cultures it isn't used. 
        published.Add(new XAttribute("Default", item.Published));

        foreach (var culture in item.AvailableCultures.OrderBy(x => x))
        {
            if (activeCultures.IsValid(culture))
            {
                published.Add(new XElement("Published", item.IsCulturePublished(culture),
                    new XAttribute("Culture", culture)));
            }
        }
        return published;
    }

    protected virtual XElement SerializeSchedule(IContent item, SyncSerializerOptions options)
    {
        var node = new XElement("Schedule");
        var schedules = contentService.GetContentScheduleByContentId(item.Id);

        var cultures = options.GetCultures();

        if (schedules != null)
        {
            foreach (var schedule in schedules.FullSchedule
                .OrderBy(x => x.Action.ToString())
                .ThenBy(x => x.Culture))
            {

                // only export if its a blank culture or one of the ones we have set. 
                if (cultures.IsValidOrBlank(schedule.Culture))
                {
                    node.Add(new XElement("ContentSchedule",
                        new XElement("Culture", schedule.Culture),
                        new XElement("Action", schedule.Action),
                        new XElement("Date", schedule.Date.ToString("s"))));
                }
            }
        }

        return node;
    }


    private XElement SerializerWriterInfo(IContent item, SyncSerializerOptions options)
    {
        var userInfoNode = new XElement("UserInfo");
        var usernames = new Dictionary<int, string>();

        userInfoNode.Add(new XElement("Writer", usernames.GetUsername(item.WriterId, userService.GetUserById!)));
        userInfoNode.Add(new XElement("Creator", usernames.GetUsername(item.CreatorId, userService.GetUserById!)));
        userInfoNode.Add(new XElement("Publisher", usernames.GetUsername(item.PublisherId, userService.GetUserById!)));

        return userInfoNode;
    }

    #endregion

    #region De-serialization

    protected override async Task<SyncAttempt<IContent>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        details.AddRange(await DeserializeBaseAsync(item, node, options));

        var infoNode = node.Element(uSyncConstants.Xml.Info);

        if (infoNode is not null)
        {
            var trashed = infoNode.Element("Trashed").ValueOrDefault(false);
            var restoreParent = infoNode.Element("Trashed")?.Attribute("Parent").ValueOrDefault(Guid.Empty) ?? Guid.Empty;
            details.AddNotNull(HandleTrashedState(item, trashed, restoreParent));
        }

		// cultures...
		


		details.AddNotNull(await DeserializeTemplate(item, node));

        var propertiesAttempt = DeserializeProperties(item, node, options);
        if (!propertiesAttempt.Success)
        {
            return SyncAttempt<IContent>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed to deserialize properties", attempt.Exception);
        }

        details.AddRange(propertiesAttempt.Result);

        if (!options.GetSetting<bool>("IgnoreSortOrder", false))
        {
            // sort order
            var sortOrder = infoNode?.Element("SortOrder").ValueOrDefault(-1) ?? -1;
            details.AddNotNull(HandleSortOrder(item, sortOrder));
        }

        var publishTimer = Stopwatch.StartNew();


        if (details.HasWarning() && options.FailOnWarnings())
        {
            // Fail on warning. means we don't save or publish because something is wrong ?
            return SyncAttempt<IContent>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed with warnings", details,
                new Exception("Import failed because of warnings, and fail on warnings is true"));
        }

        // read user ids from the xml, 
        var userId = DeserializeWriterInfo(item, node, options);

        // if the userId hasn't been set in the options , we use the one from the xml.
        if (options.UserId == -1)
        {
            options.UserId = userId;
        }

        // published status
        // this does the last save and publish
        var saveAttempt = DoSaveOrPublish(item, node, options);
        if (saveAttempt.Success)
        {
            var message = saveAttempt.Result;

            if (details.Any(x => x.Change == ChangeDetailType.Warning))
                message += $" with warning(s)";

            if (publishTimer.ElapsedMilliseconds > 10000)
            {
                message += $" (Slow publish {publishTimer.ElapsedMilliseconds}ms)";
            }

            var changeType = options.GetSetting(uSyncConstants.DefaultSettings.OnlyPublishDirty, uSyncConstants.DefaultSettings.OnlyPublishDirty_Default) && !item.IsDirty()
                ? ChangeType.NoChange : ChangeType.Import;

            // we say no change back, this stops the core second pass function from saving 
            // this item (which we have just done with DoSaveOrPublish)
            return SyncAttempt<IContent>.Succeed(item.Name ?? item.Id.ToString(), item, changeType, message ?? string.Empty, true, details);
        }
        else
        {
            return SyncAttempt<IContent>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, saveAttempt.Result ?? string.Empty, saveAttempt.Exception);
        }
    }

    protected virtual async Task<uSyncChange?> DeserializeTemplate(IContent item, XElement node)
    {
        var templateNode = node.Element("Info")?.Element("Template");

        if (templateNode != null)
        {
            var alias = templateNode.ValueOrDefault(string.Empty);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                var template = await _templateService.GetAsync(alias);
                if (template != null && template.Id != item.TemplateId)
                {
                    var oldValue = item.TemplateId;
                    item.TemplateId = template.Id;
                    return uSyncChange.Update("Template", "Template", oldValue, template.Id);
                }
            }

            var key = templateNode.ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                var template = await _templateService.GetAsync(key);
                if (template != null && template.Id != item.TemplateId)
                {
                    var oldValue = item.TemplateId;
                    item.TemplateId = template.Id;
                    return uSyncChange.Update("Template", "Template", oldValue, template.Id);
                }
            }
        }

        return null;
    }

    public int DeserializeWriterInfo(IContent item, XElement node, SyncSerializerOptions options)
    {
        var writerNode = node.Element("Info")?.Element("UserInfo");
        if (writerNode == null) return -1;

        var emails = new Dictionary<string, int>();

        item.CreatorId = emails.GetEmails(writerNode.Element("Creator").ValueOrDefault(string.Empty), userService.GetByEmail!);
        item.WriterId = emails.GetEmails(writerNode.Element("Writer").ValueOrDefault(string.Empty), userService.GetByEmail!);
        item.PublisherId = emails.GetEmails(writerNode.Element("Publisher").ValueOrDefault(string.Empty), userService.GetByEmail!);

        return item.WriterId;
    }

    /// <summary>
    ///  Second pass. 
    /// </summary>
    /// <remarks>
    ///  in v9.4 we eliminated this - but because schedules now require the content to 
    ///  exist, we need to second pass schedules, only. 99% of the time this shouldn't
    ///  have any impact. 
    /// </remarks>
    public override async Task<SyncAttempt<IContent>> DeserializeSecondPassAsync(IContent item, XElement node, SyncSerializerOptions options)
    {
        var changes = await DeserializeSchedulesAsync(item, node, options);
        if (changes.Count != 0)
            return SyncAttempt<IContent>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, "" ?? string.Empty, true, changes);

        return SyncAttempt<IContent>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.NoChange);
    }

    private async Task<List<uSyncChange>> DeserializeSchedulesAsync(IContent item, XElement node, SyncSerializerOptions options)
    {
        var changes = new List<uSyncChange>();
        var nodeSchedules = new ContentScheduleCollection();
        var currentSchedules = contentService.GetContentScheduleByContentId(item.Id);
        var cultures = options.GetDeserializedCultures(node);

        var schedules = node.Element("Info")?.Element("Schedule");
        if (schedules != null && schedules.HasElements)
        {
            logger.LogDebug("De-serialize Schedules {name}", item.Name);

            foreach (var schedule in schedules.Elements("ContentSchedule"))
            {
                var importSchedule = GetContentScheduleFromNode(schedule);
                if (cultures.IsValidOrBlank(importSchedule.Culture))
                {
                    if (importSchedule.Date < DateTime.Now)
                        continue; // don't add schedules in the past

                    logger.LogDebug("Adding {action} {culture} {date}", importSchedule.Action, importSchedule.Culture, importSchedule.Date);
                    nodeSchedules.Add(importSchedule);

                    var existing = FindSchedule(currentSchedules, importSchedule);
                    if (existing != null)
                    {
                        currentSchedules.Remove(existing);
                    }
                    currentSchedules.Add(importSchedule);
                    changes.Add(uSyncChange.Update("Schedule", $"{importSchedule.Culture} {importSchedule.Action}", "", importSchedule.Date.ToString()));
                }
            }
        }

        if (currentSchedules != null)
        {
            // remove things that are in the current but not the import. 
            var toRemove = currentSchedules.FullSchedule.Where(x => FindSchedule(nodeSchedules, x) == null)
                .ToList();

            if (toRemove.Count > 0)
                logger.LogDebug("Removing Schedules {name} ({count} to remove)", item.Name, toRemove.Count);

            foreach (var oldItem in toRemove)
            {
                if (cultures.IsValidOrBlank(oldItem.Culture))
                {
                    logger.LogDebug("Removing Schedule : {culture} {action} {date}", oldItem.Culture, oldItem.Action, oldItem.Date);
                    // only remove a culture if this serialization included it. 
                    // we don't remove things we didn't serialize. 
                    currentSchedules.Remove(oldItem);

                    changes.Add(uSyncChange.Delete("Schedule", $"{oldItem.Culture} - {oldItem.Action}", oldItem.Date.ToString()));
                }
            }

            if (changes.Count != 0)
            {
                logger.LogDebug("Saving Schedule changes: {item}", item.Name);
                await Task.Run(() => contentService.PersistContentSchedule(item, currentSchedules));
                return changes;
            }

            return [];
        }


        return [];
    }

    private static ContentSchedule GetContentScheduleFromNode(XElement scheduleNode)
    {
        var key = Guid.Empty;
        var culture = scheduleNode.Element("Culture").ValueOrDefault(string.Empty);
        var date = scheduleNode.Element("Date").ValueOrDefault(DateTime.MinValue);
        var action = scheduleNode.Element("Action").ValueOrDefault(ContentScheduleAction.Release);

        return new ContentSchedule(key, culture, date, action);
    }

    private static ContentSchedule? FindSchedule(ContentScheduleCollection currentSchedules, ContentSchedule newSchedule)
    {
        var schedule = currentSchedules.GetSchedule(newSchedule.Culture, newSchedule.Action);
        if (schedule != null && schedule.Any()) return schedule.FirstOrDefault();

        return null;
    }


    protected override uSyncChange? HandleTrashedState(IContent item, bool trashed, Guid restoreParentKey)
    {
        if (!trashed && item.Trashed)
        {
            // if the item is trashed, then the change of it's parent 
            // should restore it (as long as we do a move!)


            var restoreParentId = GetRelationParentId(item, restoreParentKey, Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias);
            contentService.Move(item, restoreParentId);

            // clean out any relations for this item (some versions of Umbraco don't do this on a Move)
            CleanRelations(item, Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias);

            return uSyncChange.Update("Restored", item.Name ?? item.Id.ToString(), "Recycle Bin", restoreParentKey.ToString());

        }
        else if (trashed && !item.Trashed)
        {
            // not already in the recycle bin?
            if (item.ParentId > Constants.System.RecycleBinContent)
            {
                // clean any relations that may be there (stops an error)
                CleanRelations(item, Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias);

                // move to the recycle bin    
                contentService.MoveToRecycleBin(item);
            }

            return uSyncChange.Update("Moved to Bin", item.Name ?? item.Id.ToString(), "", "Recycle Bin");
        }

        return null;
    }

    protected virtual Attempt<string?> DoSaveOrPublish(IContent item, XElement node, SyncSerializerOptions options)
    {
        if (options.GetSetting(uSyncConstants.DefaultSettings.OnlyPublishDirty, uSyncConstants.DefaultSettings.OnlyPublishDirty_Default) && !item.IsDirty())
        {
            logger.LogDebug("{name} not publishing because nothing is dirty [{dirty} {userDirty}]", item.Name, item.IsDirty(), item.IsAnyUserPropertyDirty());
            return Attempt.Succeed("No Changes");
        }

        var trashed = item.Trashed || (node.Element("Info")?.Element("Trashed").ValueOrDefault(false) ?? false);
        var publishedNode = node.Element("Info")?.Element("Published");
        if (!trashed && publishedNode != null)
        {
            var schedules = GetSchedules(node.Element("Info")?.Element("Schedule"));

            ContentScheduleCollection scheduleCollection = new ContentScheduleCollection();
            foreach(var schedule in schedules)
            {
                scheduleCollection.Add(schedule);
            }

			// v14 we always save now, as save and publish doesn't do that anymore...
			logger.LogDebug("Performing Save: {id} {name} {user}", item.Id, item.Name, options.UserId);
			contentService.Save(item, options.UserId, scheduleCollection);

			if (publishedNode.HasElements)
            {
                // culture based publishing.
                var cultures = options.GetDeserializedCultures(node);

                // Only unpublished cultures, when we are not already filtered by cultures
                // this stops things we don't care about this time being unpublished.
                var unpublishMissingCultures = cultures.Count == 0;

                var cultureStatuses = new Dictionary<string, uSyncContentState>();

                foreach (var culturePublish in publishedNode.Elements("Published"))
                {
                    var culture = culturePublish.Attribute("Culture").ValueOrDefault(string.Empty);

                    if (!string.IsNullOrWhiteSpace(culture) && cultures.IsValid(culture))
                    {
                        // is the item published in the config file
                        var configState = culturePublish.ValueOrDefault(false)
                            ? uSyncContentState.Published
                            : uSyncContentState.Unpublished;

                        // pending or outstanding scheduled actions can change the action we take.
                        cultureStatuses[culture] =
                            schedules.CalculateCultureState(culture, configState);
                    }

                }

                if (cultureStatuses.Count > 0)
                {
                    return PublishItem(item, cultureStatuses, unpublishMissingCultures, options.UserId);
                }
            }
            else
            {
                var state = publishedNode.Attribute("Default").ValueOrDefault(false)
                    ? uSyncContentState.Published
                    : uSyncContentState.Unpublished;

                state = schedules.CalculateCultureState(string.Empty, state);

                if (state == uSyncContentState.Published)
                {
                    return PublishItem(item, options.UserId);
                }
                else if (state == uSyncContentState.Unpublished && item.Published == true)
                {
                    contentService.Unpublish(item);
                }
            }
        }
        else
        {
			// save?
			logger.LogDebug("Performing Save (Not published): {id} {name} {user}", item.Id, item.Name, options.UserId);
			contentService.Save(item, options.UserId);

		}

		return Attempt.Succeed("Saved");
    }

    /// <summary>
    ///  work out what the current status of a given culture should be. 
    /// </summary>

    private static List<ContentSchedule> GetSchedules(XElement? schedulesNode)
    {
        var schedules = new List<ContentSchedule>();
        if (schedulesNode != null && schedulesNode.HasElements)
        {
            foreach (var schedule in schedulesNode.Elements("ContentSchedule"))
            {
                schedules.Add(GetContentScheduleFromNode(schedule));
            }
        }
        return schedules;
    }

    public Attempt<string?> PublishItem(IContent item, int userId)
    {
        try
        {
            logger.LogDebug("Publishing: {item} as User:{user}", item.Name, userId);
            var result = contentService.Publish(item, cultures: [], userId: userId);
            if (!result.Success)
            {
                var messages = result.EventMessages?.FormatMessages(",");
                logger.LogError("Failed to publish {result} [{messages}]", result.Result, messages ?? "(none)");
                if (result.InvalidProperties is not null)
                {
                    logger.LogError("Invalid Properties: {properties}", string.Join(", ", result.InvalidProperties.Select(x => x.Alias)));
                }

            }
            return result.ToAttempt();
        }
        catch (ArgumentNullException ex)
        {
            // we can get thrown a null argument exception by the notifier, 
            // which is non critical! but we are ignoring this error. ! <= 8.1.5
            if (!ex.Message.Contains("siteUri")) throw;
            return Attempt.Succeed($"Published");
        }
    }

    /// <summary>
    ///  Publish/unpublish Specified cultures for an item, and optionally un-publish missing cultures
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cultures"></param>
    /// <param name="unpublishMissing"></param>
    /// <returns></returns>
    private Attempt<string?> PublishItem(IContent item, IDictionary<string, uSyncContentState> cultures, bool unpublishMissing, int userId)
    {
        if (cultures == null) return PublishItem(item, userId);

        try
        {
            var publishedCultures = cultures
                .Where(x => x.Value == uSyncContentState.Published)
                .Select(x => x.Key)
                .ToArray();

            if (publishedCultures.Length > 0)
            {
                logger.LogDebug("Publishing {item} as {user} for {cultures}", item.Name, userId,
                    string.Join(",", publishedCultures));

                var result = contentService.Publish(item, publishedCultures, userId);

                // if this fails, we return the result
                if (!result.Success)
                {
                    var messages = result.EventMessages?.FormatMessages(",");
                    logger.LogError("Failed to publish {result} [{messages}]", result.Result, messages ?? "(none)");
                    if (result.InvalidProperties != null)
                    {
                        logger.LogError("Invalid Properties: {properties}", string.Join(", ", result.InvalidProperties.Select(x => x.Alias)));
                    }

                    return result.ToAttempt();
                }
            }

            var unpublishedCultures = cultures
                .Where(x => x.Value == uSyncContentState.Unpublished)
                .Select(x => x.Key)
                .ToArray();

            if (unpublishedCultures.Length > 0)
            {

                foreach (var culture in unpublishedCultures)
                {
                    // unpublish if the culture is currently published.
                    if (item.PublishedCultures.InvariantContains(culture))
                    {
                        logger.LogDebug("Unpublishing {item} as {user} for {culture}",
                            item.Name, userId, culture);

                        contentService.Unpublish(item, culture, userId);
                    }

                }
            }

            if (unpublishMissing)
                UnpublishMissingCultures(item, cultures.Select(x => x.Key).ToArray());

            return Attempt.Succeed("Done");
        }
        catch (ArgumentNullException ex)
        {
            // we can get thrown a null argument exception by the notifier, 
            // which is non critical! but we are ignoring this error. ! <= 8.1.5
            if (!ex.Message.Contains("siteUri")) throw;
            return Attempt.Succeed($"Published");
        }
    }
    /// <summary>
    ///  unpublish any cultures that are marked as published, in umbraco but are not published
    ///  in our *.config file.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="publishedCultures"></param>
    private void UnpublishMissingCultures(IContent item, string[] allCultures)
    {
        var missingCultures = item
            .PublishedCultures
            .Where(x => !allCultures.InvariantContains(x))
            .ToArray();

        if (missingCultures != null && missingCultures.Length > 0)
        {
            foreach (var culture in missingCultures)
            {
                logger.LogDebug("Unpublishing {item} culture not defined in config file {culture}", item.Name, culture);
                contentService.Unpublish(item, culture);
            }
        }
    }

    #endregion

    protected override Task<Attempt<IContent?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        return TaskHelper.FromResultOf(() =>
        {
            logger.LogDebug("Create: {alias} {parent} {type}", alias, parent?.Id ?? -1, itemType);
            try
            {
                var item = contentService.Create(alias, parent?.Id ?? -1, itemType);
                if (item == null)
                    return Attempt.Fail(item, new ArgumentException($"Unable to create content item of type {itemType}"));

                return Attempt.Succeed(item);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error on Create {alias}", alias);
                return Attempt.Fail<IContent?>(null, ex);
            }
        });
    }

    #region Finders

    public override Task<IContent?> FindItemAsync(Guid key)
        => Task.FromResult(contentService.GetById(key));

    protected override Task<IContent?> FindAtRootAsync(string alias)
    {
        return TaskHelper.FromResultOf<IContent?>(() =>
        {
            var rootNodes = contentService.GetRootContent();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name?.ToSafeAlias(shortStringHelper).InvariantEquals(alias) is true);
            }

            return null;
        });
    }

    #endregion

    public override Task SaveAsync(IEnumerable<IContent> items)
        => Task.FromResult(contentService.Save(items));

    public override async Task SaveItemAsync(IContent item)
        => await SaveItemAsync(item,-1);

    public Task SaveItemAsync(IContent item, int userId)
    {
        return TaskHelper.FromResultOf(() =>
        {
            try
            {
                contentService.Save(item, userId);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifier, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
            }
        });
    }

    public override Task DeleteItemAsync(IContent item)
    {
        return TaskHelper.FromResultOf(() =>
        {
            try
            {
                contentService.Delete(item);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifier, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
            }
        });
    }
}
