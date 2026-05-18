using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

public abstract class PublishableContentBaseSerializer<TObject> : ContentSerializerBase<TObject>
    where TObject : class, IPublishableContentBase
{
    protected readonly IUserService _userService;

    protected PublishableContentBaseSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentSerializerBase<TObject>> logger,
        UmbracoObjectTypes umbracoObjectType,
        SyncValueMapperCollection syncMappers,
        IUserService userService)
        : base(entityService, languageService, relationService, shortStringHelper, logger, umbracoObjectType, syncMappers)
    {
        _userService = userService;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(TObject item, SyncSerializerOptions options)
    {
        var node = InitializeNode(item, item.ContentType.Alias, options);

        var info = await SerializeInfoAsync(item, options);

        var properties = await SerializePropertiesAsync(item, options);

        node.Add(info);
        node.Add(properties);

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Id.ToString(), node, typeof(IContent), ChangeType.Export);
    }


    protected override async Task<XElement> SerializeInfoAsync(TObject item, SyncSerializerOptions options)
    {
        var info = await base.SerializeInfoAsync(item, options);

        info.Add(SerializePublishedStatus(item, options));

        info.Add(await SerializeScheduleAsync(item, options));
        info.AddIfNotNull(await SerializeTemplateAsync(item, options));

        if (options.GetSetting<bool>(uSyncConstants.DefaultSettings.IncludeUserInfo, uSyncConstants.DefaultSettings.IncludeUserInfo_Default))
        {
            info.Add(await SerializerWriterInfoAsync(item));
        }

        return info;
    }

    protected static XElement SerializePublishedStatus(TObject item, SyncSerializerOptions options)
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

    protected virtual Task<XElement?> SerializeTemplateAsync(TObject item, SyncSerializerOptions options) => Task.FromResult<XElement?>(null);

    protected abstract ContentScheduleCollection GetScheduleById(int id);

    protected virtual Task<XElement> SerializeScheduleAsync(TObject item, SyncSerializerOptions options)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var node = new XElement("Schedule");
            var schedules = GetScheduleById(item.Id);

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
        });
    }

    protected Task<XElement> SerializerWriterInfoAsync(TObject item)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var userInfoNode = new XElement("UserInfo");
            var usernames = new Dictionary<int, string>();

            userInfoNode.Add(new XElement("Writer", usernames.GetUsername(item.WriterId, _userService.GetUserById!)));
            userInfoNode.Add(new XElement("Creator", usernames.GetUsername(item.CreatorId, _userService.GetUserById!)));
            userInfoNode.Add(new XElement("Publisher", usernames.GetUsername(item.PublisherId, _userService.GetUserById!)));

            return userInfoNode;
        });
    }

    protected abstract Task<uSyncChange?> DeserializeTemplate(TObject item, XElement node);

    protected override async Task<SyncAttempt<TObject>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        details.AddRange(await DeserializeBaseAsync(item, node, options));

        details.AddNotNull(await DeserializeTemplate(item, node));

        var propertiesAttempt = await DeserializePropertiesAsync(item, node, options);
        if (!propertiesAttempt.Success)
        {
            return SyncAttempt<TObject>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed to deserialize properties", attempt.Exception);
        }

        details.AddRange(propertiesAttempt.Result);

        var publishTimer = Stopwatch.StartNew();


        if (details.HasWarning() && options.FailOnWarnings())
        {
            // Fail on warning. means we don't save or publish because something is wrong ?
            return SyncAttempt<TObject>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed with warnings", details,
                new Exception("Import failed because of warnings, and fail on warnings is true"));
        }

        // read user ids from the xml, 
        var userId = DeserializeWriterInfo(item, node);

        // if the userId hasn't been set in the options , we use the one from the xml.
        if (options.UserId == -1)
        {
            options.UserId = userId;
        }

        // published status
        // this does the last save and publish
        var saveAttempt = await DoSaveOrPublishAsync(item, node, options);
        if (saveAttempt.Success)
        {
            var message = saveAttempt.Message;

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
            return SyncAttempt<TObject>.Succeed(item.Name ?? item.Id.ToString(), saveAttempt.Content as TObject ?? item, changeType, message ?? string.Empty, true, details);
        }
        else
        {
            return SyncAttempt<TObject>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, saveAttempt.Message ?? string.Empty, saveAttempt.Exception);
        }
    }

    public int DeserializeWriterInfo(TObject item, XElement node)
    {
        var writerNode = node.Element(uSyncConstants.Xml.Info)?.Element("UserInfo");
        if (writerNode == null) return -1;

        var emails = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        item.CreatorId = emails.GetEmails(writerNode.Element("Creator").ValueOrDefault(string.Empty), _userService.GetByEmail!);
        item.WriterId = emails.GetEmails(writerNode.Element("Writer").ValueOrDefault(string.Empty), _userService.GetByEmail!);
        item.PublisherId = emails.GetEmails(writerNode.Element("Publisher").ValueOrDefault(string.Empty), _userService.GetByEmail!);

        return item.WriterId;
    }

    protected virtual async Task<SyncContentUpdateResult<TObject>> DoSaveOrPublishAsync(TObject item, XElement node, SyncSerializerOptions options)
    {
        if (options.GetSetting(uSyncConstants.DefaultSettings.OnlyPublishDirty, uSyncConstants.DefaultSettings.OnlyPublishDirty_Default) && !item.IsDirty())
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("{name} not publishing because nothing is dirty [{dirty} {userDirty}]", item.Name, item.IsDirty(), item.IsAnyUserPropertyDirty());

            return new SyncContentUpdateResult<TObject>(true, item, "No changes");
        }

        var trashed = item.Trashed || (node.Element(uSyncConstants.Xml.Info)?.Element("Trashed").ValueOrDefault(false) ?? false);
        var publishedNode = node.Element(uSyncConstants.Xml.Info)?.Element("Published");
        if (!trashed && publishedNode != null)
        {
            var schedules = GetSchedules(node.Element(uSyncConstants.Xml.Info)?.Element("Schedule"));

            var scheduleCollection = new ContentScheduleCollection();
            foreach (var schedule in schedules)
            {
                scheduleCollection.Add(schedule);
            }

            // v14 we always save now, as save and publish doesn't do that anymore...
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Performing Save: {id} {name}", item.Id, item.Name);

            var result = await SaveWithSchedulesAsync(item, options.UserId, scheduleCollection);
            if (result?.Success == true)
                item = GetByKey(item.Key) ?? item;
            else
            {
                // something went wrong saving. ???
                logger.LogWarning("Failed to save item {name} [{messages}]", item.Name, result?.EventMessages?.FormatMessages(",") ?? "(none)");
                return new SyncContentUpdateResult<TObject>(false, item, $"Failed to save {item.Name} [{result?.EventMessages?.FormatMessages(",") ?? "(none)"}]");
            }

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
                    return await PublishItemAsync(item, cultureStatuses, unpublishMissingCultures, options.UserId);
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
                    return await PublishItemAsync(item, options.UserId);
                }
                else if (state == uSyncContentState.Unpublished && item.Published == true)
                {
                    Unpublish(item, culture: null, options.UserId);
                }
            }
        }
        else
        {
            // save?
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Performing Save (Not published): {id} {name}", item.Id, item.Name);

            await SaveItemAsync(item, options.UserId);
            item = GetByKey(item.Key) ?? item;
        }

        return new SyncContentUpdateResult<TObject>(true, item, "Saved");
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

    private static ContentSchedule GetContentScheduleFromNode(XElement scheduleNode)
    {
        var key = Guid.Empty;
        var culture = scheduleNode.Element("Culture").ValueOrDefault(string.Empty);
        var date = scheduleNode.Element("Date").ValueOrDefault(DateTime.MinValue);
        var action = scheduleNode.Element("Action").ValueOrDefault(ContentScheduleAction.Release);

        return new ContentSchedule(key, culture, date, action);
    }

    /// <summary>
    ///  Publish a single item, all cultures
    /// </summary>
    public Task<SyncContentUpdateResult<TObject>> PublishItemAsync(TObject item, int userId)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Publishing: {item}", item.Name);

            var result = Publish(item, cultures: [], userId: userId);
            if (!result.Success)
            {
                var messages = result.EventMessages?.FormatMessages(",");
                logger.LogError("Failed to publish {result} [{messages}]", result.Result, messages ?? "(none)");
                if (result.InvalidProperties is not null)
                {
                    logger.LogError("Invalid Properties: {properties}", string.Join(", ", result.InvalidProperties.Select(x => x.Alias)));
                }

            }

            return Task.FromResult(result.FromPublishResult<TObject>());
        }
        catch (ArgumentNullException ex)
        {
            // we can get thrown a null argument exception by the notifier, 
            // which is non critical! but we are ignoring this error. ! <= 8.1.5
            if (!ex.Message.Contains("siteUri")) throw;

            return Task.FromResult(new SyncContentUpdateResult<TObject>
            {
                Success = true,
                Content = GetByKey(item.Key) ?? item,
                Message = "Published",
                Exception = ex
            });
        }
    }

    private async Task<SyncContentUpdateResult<TObject>> PublishItemAsync(TObject item, IDictionary<string, uSyncContentState> cultures, bool unpublishMissing, int userId)
    {
        if (cultures == null) return await PublishItemAsync(item, userId);

        try
        {
            TObject publishableItem = item;

            var publishedCultures = cultures
                .Where(x => x.Value == uSyncContentState.Published)
                .Select(x => x.Key)
                .ToArray();

            if (publishedCultures.Length > 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Publishing {item} for {cultures}", item.Name, string.Join(",", publishedCultures));

                var result = Publish(item, publishedCultures, userId);

                // if this fails, we return the result
                if (!result.Success)
                {
                    var messages = result.EventMessages?.FormatMessages(",");
                    logger.LogError("Failed to publish {result} [{messages}]", result.Result, messages ?? "(none)");
                    if (result.InvalidProperties != null)
                    {
                        logger.LogError("Invalid Properties: {properties}", string.Join(", ", result.InvalidProperties.Select(x => x.Alias)));
                    }

                    return result.FromPublishResult<TObject>();
                }

                publishableItem = result.Content as TObject ?? publishableItem;
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
                        if (logger.IsEnabled(LogLevel.Debug))
                            logger.LogDebug("Unpublishing {item} for {culture}", item.Name, culture);

                        var result = Unpublish(item, culture, userId);
                        if (result.Success)
                            publishableItem = result.Content as TObject ?? publishableItem;
                    }

                }
            }

            if (unpublishMissing)
            {
                publishableItem = UnpublishMissingCultures(item, [.. cultures.Select(x => x.Key)]);
            }

            return new SyncContentUpdateResult<TObject>(true, publishableItem, "Done");
        }
        catch (ArgumentNullException ex)
        {
            // we can get thrown a null argument exception by the notifier, 
            // which is non critical! but we are ignoring this error. ! <= 8.1.5
            if (!ex.Message.Contains("siteUri")) throw;

            return new SyncContentUpdateResult<TObject>
            {
                Success = true,
                Content = GetByKey(item.Key) ?? item,
                Message = "Published",
                Exception = ex
            };
        }
    }
    /// <summary>
    ///  unpublish any cultures that are marked as published, in umbraco but are not published
    ///  in our *.config file.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="publishedCultures"></param>
    private TObject UnpublishMissingCultures(TObject item, string[] allCultures)
    {
        TObject publishableItem = item;

        var missingCultures = item
            .PublishedCultures
            .Where(x => !allCultures.InvariantContains(x))
            .ToArray();

        if (missingCultures != null && missingCultures.Length > 0)
        {
            foreach (var culture in missingCultures)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Unpublishing {item} culture not defined in config file {culture}", item.Name, culture);

                var result = Unpublish(item, culture);
                if (result.Success)
                    publishableItem = result.Content as TObject ?? publishableItem;
            }
        }

        return publishableItem;
    }

    public abstract Task SaveItemAsync(TObject item, int userId);

    public abstract Task<OperationResult> SaveWithSchedulesAsync(TObject item, int userId, ContentScheduleCollection scheduleCollection);
    protected abstract PublishResult Unpublish(TObject item, string? culture, int userId = -1);
    protected abstract PublishResult Publish(TObject item, string[] cultures, int userId = -1);

    public override async Task<SyncAttempt<TObject>> DeserializeSecondPassAsync(TObject item, XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        details.AddRange(await this.DeserializeSecondPassSharedAsync(item, node, options,
             Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias));

        var changes = await DeserializeSchedulesAsync(item, node, options);
        if (changes.Count != 0)
            return SyncAttempt<TObject>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, "" ?? string.Empty, true,
                [.. details, .. changes]);

        // if we have changed the sort order, then we return a change, else it was no change.        
        return SyncAttempt<TObject>.Succeed(item.Name ?? item.Id.ToString(), item,
            details.Count == 0 ? ChangeType.NoChange : ChangeType.Import, details);
    }

    private async Task<List<uSyncChange>> DeserializeSchedulesAsync(TObject item, XElement node, SyncSerializerOptions options)
    {
        var changes = new List<uSyncChange>();
        var nodeSchedules = new ContentScheduleCollection();
        var currentSchedules = GetScheduleById(item.Id);
        var cultures = options.GetDeserializedCultures(node);

        var schedules = node.Element(uSyncConstants.Xml.Info)?.Element("Schedule");
        if (schedules != null && schedules.HasElements)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("De-serialize Schedules {name}", item.Name);

            foreach (var schedule in schedules.Elements("ContentSchedule"))
            {
                var importSchedule = GetContentScheduleFromNode(schedule);
                if (cultures.IsValidOrBlank(importSchedule.Culture))
                {
                    if (importSchedule.Date < DateTime.Now)
                        continue; // don't add schedules in the past

                    if (logger.IsEnabled(LogLevel.Debug))
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

            if (logger.IsEnabled(LogLevel.Debug) && toRemove.Count > 0)
                logger.LogDebug("Removing Schedules {name} ({count} to remove)", item.Name, toRemove.Count);

            foreach (var oldItem in toRemove)
            {
                if (cultures.IsValidOrBlank(oldItem.Culture))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Removing Schedule : {culture} {action} {date}", oldItem.Culture, oldItem.Action, oldItem.Date);

                    // only remove a culture if this serialization included it. 
                    // we don't remove things we didn't serialize. 
                    currentSchedules.Remove(oldItem);

                    changes.Add(uSyncChange.Delete("Schedule", $"{oldItem.Culture} - {oldItem.Action}", oldItem.Date.ToString()));
                }
            }

            if (changes.Count != 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Saving Schedule changes: {item}", item.Name);

                await PersistSchedulesAsync(item, currentSchedules);
                return changes;
            }

            return [];
        }

        return [];
    }

    private static ContentSchedule? FindSchedule(ContentScheduleCollection currentSchedules, ContentSchedule newSchedule)
    {
        var schedule = currentSchedules.GetSchedule(newSchedule.Culture, newSchedule.Action);
        if (schedule != null && schedule.Any()) return schedule.FirstOrDefault();

        return null;
    }

    protected abstract Task PersistSchedulesAsync(TObject item, ContentScheduleCollection schedules);
}
