using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

namespace uSync.BackOffice.HealthChecks;

/// <summary>
///  Checks the integrity of the uSync folder, for clashes, missing files etc...
/// </summary>
[HealthCheck("018EFC64-51BB-479B-AD09-73F538A1421A", "uSync - Folder Clash check",
    Description = "Check the integrity of the uSync folder",
    Group = "uSync")]
public class SyncFolderIntegrityChecks : HealthCheck
{
    private readonly uSyncConfigService _configService;
    private readonly SyncFileService _fileService;

    /// <summary>
    ///  Constructor 
    /// </summary>
    public SyncFolderIntegrityChecks(uSyncConfigService configService, SyncFileService fileService)
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
    public override Task<IEnumerable<HealthCheckStatus>> GetStatus()
    {
        var items = new List<HealthCheckStatus>
        {
            CheckuSyncFolder(),
            CheckConfigFolderValidity()
        };

        

        return Task.FromResult((IEnumerable<HealthCheckStatus>)items);
    }

    private HealthCheckStatus CheckuSyncFolder()
    {
        var root = _fileService.GetAbsPath(_configService.GetRootFolder());

        if (_fileService.DirectoryExists(root) is false)
        {
            return new HealthCheckStatus("No uSync folder to check")
            {
                ResultType = StatusResultType.Success
            };
        }

        var clashes = CheckFolder(root);

        if (clashes.Count > 0)
        {
            return new HealthCheckStatus($"There are {clashes.Count} clashe(s) where files share the same keys")
            {
                Description = "<p>There are multiple clashes where items of the same type share the same keys.</p>" +
                $"<ul>{string.Join("", clashes)}</ul>" +
                $"<p>To fix this perform a clean export from the uSync dashboard</p>",
                ResultType = StatusResultType.Error
            };
        }
        else
        {
            return new HealthCheckStatus("No Id clashes found")
            {
                ResultType = StatusResultType.Success,
            };
        }
    }

    private List<string> CheckFolder(string folder)
    {
        var _keys = new Dictionary<Guid, string>();

        var clashes = new List<string>();

        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

        foreach(var file in files)
        {
            try
            {
                var node = XElement.Load(file);

                if (!node.IsEmptyItem()) {
                    var key = node.GetKey();

                    var folderName = Path.GetFileName(Path.GetDirectoryName(file));
                    var fileName = Path.GetFileName(file);

                    var filepath = $"\\{folderName}\\{fileName}";

                    if (!_keys.ContainsKey(key))
                    {
                        _keys[key] = filepath;
                    }
                    else
                    {
                        clashes.Add($"<li>Clash [{filepath}] shares an id with [{_keys[key]}]</li>");
                    }
                }
            }
            catch
            {
                // we don't care its not a valid xml file. 
            }
        }

        return clashes;
    }


    private HealthCheckStatus CheckConfigFolderValidity()
    {
        var root = _fileService.GetAbsPath(_configService.GetRootFolder());

        if (_fileService.DirectoryExists(root) is false)
        {
            return new HealthCheckStatus("No uSync folder to check")
            {
                ResultType = StatusResultType.Success
            };
        }

        List<string> errors = new List<string>();

        foreach(var file in Directory.GetFiles(root, "*.config", SearchOption.AllDirectories))
        {
            try
            {
                var node = XElement.Load(file);
            }
            catch(Exception ex)
            {
                errors.Add($"<li>{Path.GetFileName(Path.GetDirectoryName(file))}\\{Path.GetFileName(file)} is invalid {ex.Message}</li>");
            }
        }

        if (errors.Count > 0)
        {
            return new HealthCheckStatus($"There are {errors.Count} Invalid .config files in the uSync folder")
            {
                Description = "<p>Some .config files are not valid xml, and will likey cause problems for a sync</p>" +
                $"<ul>{string.Join("", errors)}</ul>",
                ResultType = StatusResultType.Error
            };
        }
        else
        {
            return new HealthCheckStatus("No File errors")
            {
                ResultType = StatusResultType.Success,
            };
        }
    }

}
