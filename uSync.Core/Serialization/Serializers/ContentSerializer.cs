using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Documents;
using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;
using uSync.Core.Serialization.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
public class ContentSerializer : PublishableContentBaseSerializer<IContent>, ISyncSerializer<IContent>
{
    protected readonly IContentService contentService;

    protected readonly ITemplateService _templateService;
    protected readonly ISyncDocumentUrlCleaner? _urlCleaner;

    public ContentSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentSerializer> logger,
        IContentService contentService,
        SyncValueMapperCollection syncMappers,
        IUserService userService,
        ITemplateService templateService,
        ISyncDocumentUrlCleaner? urlCleaner)
        : base(entityService, languageService, relationService, shortStringHelper, logger, UmbracoObjectTypes.Document, syncMappers, userService)
    {
        this.contentService = contentService;

        this.relationAlias = Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias;
        _templateService = templateService;
        _urlCleaner = urlCleaner;
    }

    protected override int RecycleBinId => Constants.System.RecycleBinContent;

    #region Serialization

    protected override async Task<XElement?> SerializeTemplateAsync(IContent item, SyncSerializerOptions options)
    {
        if (item.TemplateId is null || item.TemplateId.HasValue is false)
            return new XElement(uSyncConstants.Xml.Template);

        var template = await _templateService.GetAsync(item.TemplateId.Value);
        if (template is null)
            return new XElement(uSyncConstants.Xml.Template);

        return new XElement(uSyncConstants.Xml.Template,
            new XAttribute(uSyncConstants.Xml.Key, template.Key),
            template.Alias);

    }

    protected override ContentScheduleCollection GetScheduleById(int id)
        => contentService.GetContentScheduleByContentId(id);

    #endregion

    #region De-serialization

    protected override async Task<uSyncChange?> DeserializeTemplate(IContent item, XElement node)
    {
        var templateNode = node.Element(uSyncConstants.Xml.Info)?
            .Element(uSyncConstants.Xml.Template);
        
        if (templateNode is null) return null;

        var alias = templateNode.ValueOrDefault(string.Empty);
        var key = templateNode.GetKey();

        var template = await _templateService.GetAsync(alias) 
            ?? await _templateService.GetAsync(key); 

        if (template is null || template.Id == item.TemplateId) return null;

        var oldValue = item.TemplateId;
        item.TemplateId = template.Id;
        return uSyncChange.Update("Template", "Template", oldValue, template.Id);
    }

    // trashed helpers. 
    protected override void MoveToRecycleBin(IContent item) => contentService.MoveToRecycleBin(item);
    protected override void SetTrashed(IContent item) => ((ContentBase)item).Trashed = true;
    protected override void MoveItem(IContent item, int parentId) => contentService.Move(item, parentId);
    protected override IContent? GetByKey(Guid id) => contentService.GetById(id);

    public override Task<OperationResult> SaveWithSchedulesAsync(IContent item, int userId, ContentScheduleCollection scheduleCollection)
        => Task.FromResult(contentService.Save(item, userId, scheduleCollection));
    #endregion

    protected override Task<Attempt<IContent?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            if (logger.IsEnabled(LogLevel.Debug))
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
        return uSyncTaskHelper.FromResultOf<IContent?>(() =>
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

    protected override Task<SyncParentItem?> FindParentByIdAsync(int id)
    {
        return uSyncTaskHelper.FromResultOf<SyncParentItem?>(() =>
        {
            var parent = contentService.GetById(id);
            if (parent != null)
            {
                return new SyncParentItem
                {
                    Id = parent.Id,
                    Key = parent.Key,
                    Name = parent.Name ?? parent.Id.ToString()
                };
            }
            return null;
        });
    }

    public override Task SaveAsync(IEnumerable<IContent> items)
        => Task.FromResult(contentService.Save(items));

    public override async Task SaveItemAsync(IContent item)
        => await SaveItemAsync(item, -1);

    protected override Task PersistSchedulesAsync(IContent item, ContentScheduleCollection schedules)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            contentService.PersistContentSchedule(item, schedules);
        });
    }

    public override Task SaveItemAsync(IContent item, int userId)
    {
        return uSyncTaskHelper.FromResultOf(() =>
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

    protected override PublishResult Unpublish(IContent item, string? culture, int userId = -1)
        => contentService.Unpublish(item, culture, userId);

    protected override PublishResult Publish(IContent item, string[] cultures, int userId = -1)
        => contentService.Publish(item, cultures, userId);

    public override Task DeleteItemAsync(IContent item)
    {
        return uSyncTaskHelper.FromResultOf(() =>
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

    protected override Task OnKeyChange(IContent item, Guid oldKey, Guid newKey)
    {
        // key changes need to clean the DocumentUrl cache.
        _urlCleaner?.CleanUrlsForDocument(oldKey);
        return Task.CompletedTask;
    }
}

