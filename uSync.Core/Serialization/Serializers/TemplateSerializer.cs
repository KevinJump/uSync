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
    private readonly IFileSystem? _viewFileSystem;

    private readonly ITemplateService _templateService;
    private readonly IUserIdKeyResolver _userIdKeyResolver;

    private readonly uSyncCapabilityChecker _capabilityChecker;
    private readonly IConfiguration _configuration;

    [ActivatorUtilitiesConstructor]
    public TemplateSerializer(
        IEntityService entityService,
        ILogger<TemplateSerializer> logger,
        // shortStringHelper is no longer used, but is kept so we don't break the constructor signature.
        IShortStringHelper shortStringHelper,
        FileSystems fileSystems,
        IConfiguration configuration,
        uSyncCapabilityChecker capabilityChecker,
        ITemplateService templateService,
        IUserIdKeyResolver userIdKeyResolver)
        : base(entityService, logger)
    {
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
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Processing deletes as part of the last pass");

            return await base.ProcessDeleteAsync(key, alias, flags);
        }

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Delete not processing as this is not the final pass");

        return SyncAttempt<ITemplate>.Succeed(alias, ChangeType.Hidden);
    }

    private async Task<ITemplate?> FindTemplateFromNodeAsync(XElement node)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();

        var item = default(ITemplate);

        if (key != Guid.Empty)
            item = await FindItemAsync(key);

        return item ?? await FindItemAsync(alias);

    }

    protected override async Task<SyncAttempt<ITemplate>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var key = node.GetKey();
        var alias = node.GetAlias();
        var name = node.Element("Name").ValueOrDefault(string.Empty);

        var details = new List<uSyncChange>();

        var item = await FindTemplateFromNodeAsync(node);
        if (item is null)
        {
            // we only need the content when we are creating the template, when we are updating
            // it either comes from the node (below) or it is already on disk.
            var contentAttempt = await GetContentForTemplateAsync(node, options);
            if (!contentAttempt) return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, contentAttempt.Exception?.Message ?? "Failed to get content");

            var userKey = await _userIdKeyResolver.GetAsync(options.UserId);
            var attempt = await _templateService.CreateAsync(
                name,
                alias,
                contentAttempt.Result,
                userKey, key);

            if (attempt.Success is false) 
                return SyncAttempt<ITemplate>.Fail(name, attempt.Result, ChangeType.Import,
                    $"Failed to create template: {attempt.Status}",
                    new InvalidOperationException($"Failed to create template '{alias}': {attempt.Status} {attempt.Exception?.Message ?? "Unknown error"}"));

            item = attempt.Result;
            details.AddNew(alias, alias, "Template");

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("New Template: {alias} {path}", item.Alias, item.Path);

            // don't need to go through the process, the create also saves it.
            return SyncAttempt<ITemplate>.Succeed(name, item, ChangeType.Import, "Created", true, details);
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

        // v16+ if the views are compiled, then we never actually see them get written to disk.
        // so we are not going to mark them. Later on in the second pass we will delete any empty 
        // templates while in razor view mode - but that is all. 

        return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, details);
    }

    private async Task<Attempt<string?>> GetContentForTemplateAsync(XElement node, SyncSerializerOptions options)
    {
        // if the setup is configured this way, we get template content from the xml file directly. 
        if (ShouldGetContentFromNode(node, options))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Getting content for Template from XML");

            return Attempt.Succeed(GetContentFromConfig(node));
        }

        // if not, try and fetch the content the way umbraco does (via the template service)
        var templateFileName = node.GetAlias();
        if (templateFileName.EndsWith(".cshtml", StringComparison.InvariantCultureIgnoreCase) is false)
            templateFileName = $"{templateFileName}.cshtml";

        // note: the template service returns Stream.Null (not null) when the file is missing,
        // and an empty file tells us nothing - so both mean 'look somewhere else'.
        var stream = await _templateService.GetFileContentStreamAsync(templateFileName);
        if (stream is not null && stream != Stream.Null)
        {
            await using (stream)
            {
                using var sr = new StreamReader(stream);
                var fileContent = await sr.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(fileContent) is false)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Reading {path} contents from template service", templateFileName);

                    return Attempt.Succeed(fileContent);
                }
            }
        }

        // if not - then old-school, attempt to get content from the viewFileSystem
        var templatePath = ViewPath(node.GetAlias());
        if (templatePath is not null && _viewFileSystem?.FileExists(templatePath) is true)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Reading {path} contents", templatePath);

            return Attempt.Succeed(GetContentFromFile(templatePath));
        }

        // if we get here, we've failed to get the content from anywhere, so it might be missing or its 
        // compiled into the site, and in some dll (although the template service should fetch this?)

        if (ViewsAreCompiled(options) is true)
        {
            // template is not on disk, we could use the viewEngine to find the view 
            // if this finds the view it tells us that the view is somewhere else ? 
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Failed to find content, but UsingRazorViews so if Umbraco creates anyway, we will then delete the file");

            // internally Umbraco parses the content for the master (from the Layout value) so we
            // need to fake that. 
            var master = node.Element("Parent")?.ValueOrDefault(string.Empty);
            var layout = string.IsNullOrWhiteSpace(master) ? "null" : $"\"{master}.cshtml\"";

            return Attempt.Succeed($"@{{\n    Layout = {layout};\n}}\n" +
                $"<!-- [uSyncMarker:{this.Id}]  template content - will be removed -->");
        }

        // template is missing and the views are not compiled , then we can't create.
        logger.LogWarning("Failed to create template {path} the local file is missing", templatePath);
        return Attempt.Fail("", new Exception($"The template {templatePath} file is missing."));
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
        // no content in the file, so there is nothing to take from it.
        if (node.Element("Contents") is null) return false;

        // on a version of Umbraco that has runtime modes, we don't import the content
        // in Production, because the views will be compiled into the site.
        if (_capabilityChecker.HasRuntimeMode && ViewsAreCompiled(options))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Template contents will not be imported because site is running in Production mode");

            return false;
        }

        // note: we don't check the IncludeContent setting here - that setting controls
        // whether we *export* the content, if it's in the file we will import it.
        return true;
    }

    public static string GetContentFromConfig(XElement node)
        => node.Element("Contents").ValueOrDefault(string.Empty);

    private string GetContentFromFile(string templatePath)
    {
        try
        {
            var templateFilePath = _viewFileSystem?.GetFullPath(templatePath);
            if (System.IO.File.Exists(templateFilePath) is true)
            {
                // read it locally, (quicker less likely to lock).
                return System.IO.File.ReadAllText(templateFilePath);
            }
        }
        catch (Exception ex)
        {
            // any failure here is not fatal, we fall back to the filesystem provider below.
            logger.LogWarning(ex, "Error reading template, will read from filesystem provider instead");
        }

        // via the file system, which does work, but occasionaly it locks.
        using var stream = _viewFileSystem?.OpenFile(templatePath);
        if (stream is null) return string.Empty;

        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    public override async Task<SyncAttempt<ITemplate>> DeserializeSecondPassAsync(ITemplate item, XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();
        var saved = true;

        if (ViewsAreCompiled(options))
        {
            // using razor views - we delete the template file at the end (because its in a razor view). 
            var templatePath = ViewPath(item.Alias);
            if (templatePath is not null && _viewFileSystem?.FileExists(templatePath) is true)
            {
                var fullPath = _viewFileSystem.GetFullPath(templatePath);

                if (System.IO.File.Exists(fullPath))
                {
                    var content = await System.IO.File.ReadAllTextAsync(fullPath);
                    if (content.Contains($"[uSyncMarker:{this.Id}]"))
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
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

        if (item.HasIdentity)
        {
            // update
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Saving: {alias} {path}", item.Alias, item.Path);

            var result = await _templateService.UpdateAsync(item, userKey);

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Update Template Result: [{key}] {result} {status}", item.Key, result.Success, result.Status);

            if (result.Success is false)
            {
                throw new InvalidOperationException(
                    $"Could not save template {item.Alias}: {result.Status}");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Creating: {alias} {path}", item.Alias, item.Path);

            var result = await _templateService.CreateAsync(item.Name ?? item.Alias, item.Alias, item.Content, userKey, item.Key);

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Update Template Result: [{key}] {result} {status}", item.Key, result.Success, result.Status);

            if (result.Success is false)
            {
                throw new InvalidOperationException(
                    $"Could not save template {item.Alias}: {result.Status}");
            }
        }
    }

    public override async Task SaveAsync(IEnumerable<ITemplate> items)
    {
        foreach (var item in items)
            await SaveItemAsync(item);
    }

    public override async Task DeleteItemAsync(ITemplate item)
        => await _templateService.DeleteAsync(item.Alias, Constants.Security.SuperUserKey);

    public override string ItemAlias(ITemplate item)
        => item.Alias;

    // Umbraco names the view file from the alias verbatim (see TemplateRepository.SetVirtualPath)
    // so we have to do the same, or we look for/delete the wrong file for aliases with spaces.
    private string? ViewPath(string alias)
        => _viewFileSystem?.GetRelativePath(alias + ".cshtml");

    private bool ViewsAreCompiled(SyncSerializerOptions options)
        => _configuration.IsUmbracoRunningInProductionMode()
            || options.GetSetting(uSyncConstants.DefaultSettings.UsingRazorViews, uSyncConstants.DefaultSettings.UsingRazorViews_Default);
}
