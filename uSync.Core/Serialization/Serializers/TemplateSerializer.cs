using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;
using uSync.Core.Versions;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("D0E0769D-CCAE-47B4-AD34-4182C587B08A", "Template Serializer", uSyncConstants.Serialization.Template)]
public class TemplateSerializer : SyncSerializerBase<ITemplate>, ISyncSerializer<ITemplate>
{
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IFileSystem? _viewFileSystem;

    private readonly ITemplateService _templateService;
    private readonly IUserIdKeyResolver _userIdKeyResolver;

    private readonly uSyncCapabilityChecker _capabilityChecker;
    private readonly IConfiguration _configuration;

    [ActivatorUtilitiesConstructor]
    public TemplateSerializer(
        IEntityService entityService,
        ILogger<TemplateSerializer> logger,
        IShortStringHelper shortStringHelper,
        FileSystems fileSystems,
        IConfiguration configuration,
        uSyncCapabilityChecker capabilityChecker,
        ITemplateService templateService,
        IUserIdKeyResolver userIdKeyResolver)
        : base(entityService, logger)
    {
        _shortStringHelper = shortStringHelper;

        _viewFileSystem = fileSystems.MvcViewsFileSystem;
        _configuration = configuration;
        _capabilityChecker = capabilityChecker;
        _templateService = templateService;
        _userIdKeyResolver = userIdKeyResolver;
    }

    protected override async Task<SyncAttempt<ITemplate>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        if (flags.HasFlag(SerializerFlags.LastPass))
        {
            logger.LogDebug("Processing deletes as part of the last pass");
            return await base.ProcessDeleteAsync(key, alias, flags);
        }

        logger.LogDebug("Delete not processing as this is not the final pass");
        return SyncAttempt<ITemplate>.Succeed(alias, ChangeType.Hidden);
    }

    protected override async Task<SyncAttempt<ITemplate>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();

        var name = node.Element("Name").ValueOrDefault(string.Empty);
        var item = default(ITemplate);

        var details = new List<uSyncChange>();

        if (key != Guid.Empty)
            item = await FindItemAsync(key);

        item ??= await FindItemAsync(alias);

        if (item == null)
        {
            item = new Template(_shortStringHelper, name, alias);
            details.AddNew(alias, alias, "Template");

            if (ShouldGetContentFromNode(node, options))
            {
                logger.LogDebug("Getting content for Template from XML");
                item.Content = GetContentFromConfig(node);
            }
            else
            {
                logger.LogDebug("Loading template content from disk");

                var templatePath = ViewPath(alias);
                if (templatePath is not null && _viewFileSystem?.FileExists(templatePath) is true)
                {
                    logger.LogDebug("Reading {path} contents", templatePath);
                    item.Content = GetContentFromFile(templatePath);
                    item.Path = templatePath;
                }
                else
                {
                    if (!ViewsAreCompiled(options))
                    {
                        // template is missing
                        // we can't create 
                        logger.LogWarning("Failed to create template {path} the local file is missing", templatePath);
                        return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, $"The template {templatePath} file is missing.");
                    }
                    else
                    {
                        // template is not on disk, we could use the viewEngine to find the view 
                        // if this finds the view it tells us that the view is somewhere else ? 

                        logger.LogDebug("Failed to find content, but UsingRazorViews so will create anyway, then delete the file");
                        item.Content = $"<!-- [uSyncMarker:{this.Id}]  template content - will be removed -->";
                    }
                }
            }
        }

        if (item == null)
        {
            // creating went wrong
            logger.LogWarning("Failed to create template");
            return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Failed to create template");
        }

        if (item.Key != key)
        {
            details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);
            item.Key = key;
        }

        if (item.Name != name)
        {
            details.AddUpdate(uSyncConstants.Xml.Name, item.Name ?? string.Empty, name);
            item.Name = name;
        }

        if (item.Alias != alias)
        {
            details.AddUpdate(uSyncConstants.Xml.Alias, item.Alias, alias);
            item.Alias = alias;
        }

        if (ShouldGetContentFromNode(node, options))
        {
            var content = GetContentFromConfig(node);
            if (content != item.Content)
            {
                details.AddUpdate("Content", item.Content ?? string.Empty, content);
                item.Content = content;
            }
        }

        //var master = node.Element("Parent").ValueOrDefault(string.Empty);
        //if (master != string.Empty)
        //{
        //    var masterItem = fileService.GetTemplate(master);
        //    if (masterItem != null)
        //        item.SetMasterTemplate(masterItem);
        //}

        // Deserialize now takes care of the save.
        // fileService.SaveTemplate(item);

        return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, details);
    }

    /// <summary>
    ///  As a default if the file contains the content node, then we are going to use it 
    ///  for the content. if it doesn't then we are not.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private bool ShouldGetContentFromNode(XElement node, SyncSerializerOptions options)
    {
        if (_capabilityChecker != null
            && _configuration != null
            && _capabilityChecker.HasRuntimeMode)
        {
            if (node.Element("Contents") != null)
            {
                if (ViewsAreCompiled(options))
                {
                    logger.LogDebug("Template contents will not be imported because site is running in Production mode");
                    return false;
                }

                // else (we have content - not running in Production) 
                return true;
            }

            // else (we don't have content it doesn't matter)
            return false;
        }

        // default 
        return node.Element("Contents") != null;
        // && options.GetSetting(uSyncConstants.Conventions.IncludeContent, false);
    }

    public static string GetContentFromConfig(XElement node)
        => node.Element("Contents").ValueOrDefault(string.Empty);

    public string GetContentFromFile(string templatePath)
    {
        var content = "";
        using (var stream = _viewFileSystem?.OpenFile(templatePath))
        {
            if (stream is null) return content;

            using (var sr = new StreamReader(stream))
            {
                content = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();
            }

            stream.Close();
            stream.Dispose();
        }

        return content;
    }

    public override async Task<SyncAttempt<ITemplate>> DeserializeSecondPassAsync(ITemplate item, XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();
        var saved = false;

        var master = node.Element("Parent").ValueOrDefault(string.Empty);
        if (master != string.Empty && item.MasterTemplateAlias != master)
        {
            logger.LogDebug("Looking for master {master}", master);
            var masterItem = await FindItemAsync(master);
            if (masterItem != null && item.MasterTemplateAlias != master)
            {
                details.AddUpdate("Parent", item.MasterTemplateAlias ?? string.Empty, master);

                logger.LogDebug("Setting Master {alias}", masterItem.Alias);
                // item.SetMasterTemplate(masterItem);

                await SaveItemAsync(item);
                saved = true;
            }
        }

        if (ViewsAreCompiled(options))
        {
            // using razor views - we delete the template file at the end (because its in a razor view). 
            var templatePath = ViewPath(item.Alias);
            if (templatePath is not null && _viewFileSystem?.FileExists(templatePath) is true)
            {
                var fullPath = _viewFileSystem.GetFullPath(templatePath);

                if (System.IO.File.Exists(fullPath))
                {
                    var content = System.IO.File.ReadAllText(fullPath);
                    if (content.Contains($"[uSyncMarker:{this.Id}]"))
                    {
                        logger.LogDebug("Removing the file from disk, because it exists in a razor view {templatePath}", templatePath);
                        _viewFileSystem.DeleteFile(templatePath);

                        // we have to tell the handlers we saved it - or they will and write the file back 
                        return SyncAttempt<ITemplate>.Succeed(item.Name!, item, ChangeType.Import, "Razor view removed", true, details);
                    }
                }
            }
        }

        return SyncAttempt<ITemplate>.Succeed(item.Name!, item, ChangeType.Import, "", saved, details);
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(ITemplate item, SyncSerializerOptions options)
    {
        var node = this.InitializeBaseNode(item, item.Alias, await this.CalculateLevelAsync(item));

        node.Add(new XElement("Name", item.Name));
        node.Add(new XElement("Parent", item.MasterTemplateAlias));

        if (options.GetSetting(uSyncConstants.Conventions.IncludeContent, false))
        {
            node.Add(SerializeContent(item));
        }

        return SyncAttempt<XElement>.Succeed(item.Name!, node, typeof(ITemplate), ChangeType.Export);
    }

    private static XElement SerializeContent(ITemplate item)
        => new("Contents", new XCData(item.Content ?? string.Empty));


    private async Task<int> CalculateLevelAsync(ITemplate item)
    {
        if (item.MasterTemplateAlias.IsNullOrWhiteSpace()) return 1;

        int level = 1;
        var current = item;
        while (!string.IsNullOrWhiteSpace(current.MasterTemplateAlias) && level < 20)
        {
            level++;
            var parent = await FindItemAsync(current.MasterTemplateAlias);
            if (parent == null) return level;

            current = parent;
        }

        return level;
    }

    public override async Task<ITemplate?> FindItemAsync(string alias)
        => await _templateService.GetAsync(alias);

    public override async Task<ITemplate?> FindItemAsync(Guid key)
        => await _templateService.GetAsync(key);

    public override async Task SaveItemAsync(ITemplate item)
    {
        var userKey = Constants.Security.SuperUserKey;

        var existing = await _templateService.GetAsync(item.Alias);
        if (existing is not null)
        {
            item.Key = existing.Key;
            item.Id = existing.Id;
            item.CreateDate = existing.CreateDate;
            item.UpdateDate = existing.UpdateDate;
        }

        logger.LogDebug("Save Template {name} {alias} [{contentLength}] {userKey} {key}", item.Name, item.Alias, item.Content?.Length ?? 0, userKey, item.Key);

        if (existing is null) {
            var result = await _templateService.CreateAsync(item.Name ?? item.Alias, item.Alias, item.Content, userKey, item.Key);
            logger.LogDebug("Create Template Result: [{key}] {result} {status}", result.Result.Key, result.Success, result.Status);
        }
        else
        {
            var result = await  _templateService.UpdateAsync(item, userKey);
            logger.LogDebug("Update Template Result: [{key}] {result} {status}", item.Key, result.Success, result.Status);
        }

        var templates = await _templateService.GetAllAsync();
        logger.LogDebug("[Templates]: {count} {names}",
               templates.Count(), string.Join(",", templates.Select(x => $"{x.Alias}-{x.Key}")));
    }

    public override async Task SaveAsync(IEnumerable<ITemplate> items)
    {
        foreach(var item in items)
            await SaveItemAsync(item);
    }

    public override async Task DeleteItemAsync(ITemplate item)
        => await _templateService.DeleteAsync(item.Alias, Constants.Security.SuperUserKey);

    public override string ItemAlias(ITemplate item)
        => item.Alias;

    /// <summary>
    ///  we clean the content out of the template,
    ///  We don't care if the content has changed during a normal serialization
    /// </summary>
    protected override XElement CleanseNode(XElement node)
    {
        node.Element("Content")?.Remove();
        return base.CleanseNode(node);
    }


    private string? ViewPath(string alias)
        => _viewFileSystem?.GetRelativePath(alias.Replace(" ", "") + ".cshtml");

    private bool ViewsAreCompiled(SyncSerializerOptions options)
        => _configuration.IsUmbracoRunningInProductionMode()
            || options.GetSetting("UsingRazorViews", false);
}
