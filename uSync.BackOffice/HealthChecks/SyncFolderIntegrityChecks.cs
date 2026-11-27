using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Umbraco.Cms.Core.HealthChecks;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.FolderChecks;

namespace uSync.BackOffice.HealthChecks;

/// <summary>
///  Checks the integrity of the uSync folder, for clashes, missing files etc...
/// </summary>
/// <remarks>
///  the checks themselves live in uSync.FolderChecks, so the uSync command line
///  (files-verify) finds the same problems in a pipeline.
/// </remarks>
[HealthCheck("018EFC64-51BB-479B-AD09-73F538A1421A", "uSync - Folder Clash check",
    Description = "Check the integrity of the uSync folder",
    Group = "uSync")]
public class SyncFolderIntegrityChecks : HealthCheck
{
    private readonly ISyncConfigService? _configService;
    private readonly ISyncFileService? _fileService;

    public SyncFolderIntegrityChecks() { }

    /// <summary>
    ///  Constructor
    /// </summary>
    public SyncFolderIntegrityChecks(ISyncConfigService configService, ISyncFileService fileService)
    {
        _configService = configService;
        _fileService = fileService;
    }

    /// <inheritdoc/>
    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
    {
        throw new InvalidOperationException("No Actions");
    }

    /// <inheritdoc/>
    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
    {
        if (_configService is null || _fileService is null)
            return Task.FromResult(Enumerable.Empty<HealthCheckStatus>());

        return Task.FromResult<IEnumerable<HealthCheckStatus>>(CheckFolder());
    }

    private List<HealthCheckStatus> CheckFolder()
    {
        if (_configService is null || _fileService is null)
            return [new HealthCheckStatus("Unable to check uSync folder integrity")];

        var root = _fileService.GetAbsPath(_configService.GetWorkingFolder());

        if (_fileService.DirectoryExists(root) is false)
        {
            return [new HealthCheckStatus("No uSync folder to check")
            {
                ResultType = StatusResultType.Success
            }];
        }

        SyncFolderVerifyResult result;
        try
        {
            result = new SyncFolderVerifier().Verify(root, new SyncFolderVerifyOptions { IncludeContent = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SyncFolderNotFoundException)
        {
            return [new HealthCheckStatus($"Unable to check uSync folder integrity: {ex.Message}")
            {
                ResultType = StatusResultType.Warning
            }];
        }

        return
        [
            GetStatus(result, SyncFileRules.DuplicateKey,
                count => $"There are {count} item(s) that share a key with another item",
                "<p>Items of the same type share the same key, so only one of them can be imported.</p>",
                "<p>To fix this perform a clean export from the uSync dashboard</p>",
                "No Id clashes found"),

            GetStatus(result, SyncFileRules.InvalidXml,
                count => $"There are {count} Invalid .config files in the uSync folder",
                "<p>Some .config files are not valid xml, and will likely cause problems for a sync</p>",
                string.Empty,
                "No File errors"),

            GetStatus(result, SyncFileRules.MergeConflict,
                count => $"There are {count} unresolved merge conflict(s) in the uSync folder",
                "<p>Some files still contain git merge conflict markers (&lt;&lt;&lt;&lt;&lt;&lt;&lt;, =======, &gt;&gt;&gt;&gt;&gt;&gt;&gt;).</p>",
                "<p>Resolve the conflicts in source control, or perform a clean export from the uSync dashboard. Markers inside a value (e.g. block list json) still load, so uSync would import the conflicted value as it is.</p>",
                "No merge conflicts found"),

            GetStatus(result, SyncFileRules.AliasClash,
                count => $"There are {count} item(s) that share an alias with a different key",
                "<p>Items of the same type have the same alias but different keys, only one of them can be imported. This often happens when two branches each create the same item.</p>",
                "<p>Remove one of the files and use the other item's key, then perform a clean export from the uSync dashboard</p>",
                "No alias clashes found"),

            GetStatus(result, SyncFileRules.DeleteAndLive,
                count => $"There are {count} item(s) that are both deleted and present",
                "<p>Items are marked as deleted, but there is also a file for the same key, so an import would both create and delete them.</p>",
                "<p>Remove the delete file or the item file, depending on which you want to keep</p>",
                "No deleted items clash with live items"),
        ];
    }

    private static HealthCheckStatus GetStatus(SyncFolderVerifyResult result, SyncFileRule rule,
        Func<int, string> failMessage, string intro, string fix, string successMessage)
    {
        var issues = result.Issues.Where(x => x.Rule == rule).ToList();

        if (issues.Count == 0)
        {
            return new HealthCheckStatus(successMessage)
            {
                ResultType = StatusResultType.Success,
            };
        }

        var items = issues.Select(x =>
        {
            var path = SyncFolderVerifier.GetRelativePath(result.Folder, x.FilePath);
            var location = x.Line > 0 ? $"{path}:{x.Line}" : path;
            return $"<li>[{WebUtility.HtmlEncode(location)}] {WebUtility.HtmlEncode(x.Message)}</li>";
        });

        // the details go in the message, the management API doesn't return the description,
        // and the backoffice renders the message as html.
        return new HealthCheckStatus($"{failMessage(issues.Count)}{intro}<ul>{string.Join("", items)}</ul>{fix}")
        {
            ResultType = rule.Severity == SyncFileSeverity.Error ? StatusResultType.Error : StatusResultType.Warning
        };
    }
}
