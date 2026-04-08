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
///  handles creation of templates, especially when running in production mode.
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
    ///  creates a template, using the service when possible and falling back to the repository
    ///  when already in production mode or when the service returns <see cref="TemplateOperationStatus.NotAllowedInProductionMode"/>.
    /// </summary>
    /// <param name="name">The display name of the template.</param>
    /// <param name="alias">The alias of the template.</param>
    /// <param name="content">The template content.</param>
    /// <param name="userKey">The key of the user performing the operation.</param>
    /// <param name="key">The key to assign to the new template.</param>
    /// <returns></returns>
    public async Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(string name, string alias, string? content, Guid userKey, Guid key)
    {
        if (IsInProductionMode() is false)
        {
            var attempt = await _templateService.CreateAsync(name, alias, content, userKey, key);
            if (attempt.Success)
                return attempt;

            // only fall back to the repository when blocked by production mode restrictions
            if (attempt.Status is not TemplateOperationStatus.NotAllowedInProductionMode)
                return attempt;
        }

        // in production mode (or blocked by it) - use the repository directly
        // https://github.com/umbraco/Umbraco-CMS/pull/21600#issuecomment-4205232583
        return await CreateTemplateInternal(name, alias, content, userKey, key);
    }

    private Task<Attempt<ITemplate, TemplateOperationStatus>> CreateTemplateInternal(string name, string alias, string? content, Guid userKey, Guid key)
    {
        var template = new Template(_shortStringHelper, name, alias)
        {
            Content = content,
            Key = key,
        };

        try
        {
            using var scope = _scopeProvider.CreateCoreScope(autoComplete: true);
            _templateRepository.Save(template);

            return Task.FromResult(Attempt.SucceedWithStatus<ITemplate, TemplateOperationStatus>(TemplateOperationStatus.Success, template));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Attempt.FailWithStatus<ITemplate, TemplateOperationStatus>(TemplateOperationStatus.NotAllowedInProductionMode, template, ex));
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
