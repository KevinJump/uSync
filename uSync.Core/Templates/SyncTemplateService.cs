using Microsoft.Extensions.Configuration;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Versions;

namespace uSync.Core.Templates;

/// <summary>
///  does creating of templates, espeically if we are in production mode. 
/// </summary>
internal class SyncTemplateService : ISyncTemplateService
{
    private readonly ITemplateService _templateService;
    private readonly IConfiguration _configuration;
    private readonly ICoreScopeProvider _scopeProvider;

    private readonly ITemplateRepository _templateRepository;
    private readonly IShortStringHelper _shortStringHelper;

    public SyncTemplateService(ITemplateService templateService, IConfiguration configuration, ICoreScopeProvider scopeProvider, ITemplateRepository templateRepository, IShortStringHelper shortStringHelper)
    {
        _templateService = templateService;
        _configuration = configuration;
        _scopeProvider = scopeProvider;
        _templateRepository = templateRepository;
        _shortStringHelper = shortStringHelper;
    }

    /// <summary>
    ///  creates a template, but only if we are in production mode. 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="alias"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public async Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(string name, string alias, string? content, Guid userKey, Guid key)
    {
        TemplateOperationStatus lastKnownStatus = TemplateOperationStatus.Success;

        if (IsInProductionMode() is false)
        {
            var attempt = await _templateService.CreateAsync(name, alias, content, userKey, key);
            if (attempt.Success)
                return attempt;
        }

        // else - lets try the repository way (surely this is a hack?)
        // https://github.com/umbraco/Umbraco-CMS/pull/21600#issuecomment-4205232583
        return await CreateTemplateInternal(name, alias, content, userKey, key, lastKnownStatus);
    }

    private async Task<Attempt<ITemplate, TemplateOperationStatus>> CreateTemplateInternal(string name, string alias, string? content, Guid userKey, Guid key, TemplateOperationStatus lastKnownStatus) 
    {
        var template = new Template(_shortStringHelper, name, alias)
        {
            Content = content,
            Key = key,
        };

        try
        {
            using (var scope = _scopeProvider.CreateCoreScope(autoComplete: true))
            {
                _templateRepository.Save(template);
                scope.Complete();

                return Attempt.SucceedWithStatus<ITemplate, TemplateOperationStatus>(TemplateOperationStatus.Success, template);
            }
        }
        catch (Exception ex)
        {
            return Attempt.FailWithStatus<ITemplate, TemplateOperationStatus>(lastKnownStatus, template, ex);
        }
    }


    public async Task<ITemplate?> GetAsync(Guid key)
        => await _templateService.GetAsync(key);

    public async Task<ITemplate?> GetAsync(string alias)
        => await _templateService.GetAsync(alias);

    public async Task<ITemplate?> GetAsync(int id)
        => await _templateService.GetAsync(id);

    public async Task<Attempt<ITemplate, TemplateOperationStatus>> UpdateAsync(ITemplate template, Guid userKey)
        => await _templateService.UpdateAsync(template, userKey);

    public async Task DeleteAsync(string alias, Guid userKey)
        => await _templateService.DeleteAsync(alias, userKey);

    public async Task<IEnumerable<ITemplate>> GetChildrenAsync(int templateId)
        => await _templateService.GetChildrenAsync(templateId);

    private bool IsInProductionMode()
      => _configuration.IsUmbracoRunningInProductionMode();
}
