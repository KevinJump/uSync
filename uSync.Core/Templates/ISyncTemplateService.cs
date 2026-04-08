using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace uSync.Core.Templates;

public interface ISyncTemplateService
{
    Task<Attempt<ITemplate, TemplateOperationStatus>> CreateAsync(string name, string alias, string? content, Guid userKey, Guid key);
    Task DeleteAsync(string alias, Guid userKey);
    Task<ITemplate?> GetAsync(string alias);
    Task<ITemplate?> GetAsync(Guid key);
    Task<ITemplate?> GetAsync(int id);
    Task<IEnumerable<ITemplate>> GetChildrenAsync(int templateId);
    Task<Attempt<ITemplate, TemplateOperationStatus>> UpdateAsync(ITemplate template, Guid userKey);
}