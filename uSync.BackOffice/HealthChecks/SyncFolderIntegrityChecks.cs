using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.HealthChecks;

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
    private readonly ISyncFileService _fileService;

    /// <summary>
    ///  Constructor 
    /// </summary>
    public SyncFolderIntegrityChecks(uSyncConfigService configService, ISyncFileService fileService)
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
        var root = _fileService.GetAbsPath(_configService.GetWorkingFolder());

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
            return new HealthCheckStatus($"There are {clashes.Count} clash(es) where files share the same keys")
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

    private static List<string> CheckFolder(string folder)
    {
        var _keys = new Dictionary<Guid, string>();

        var clashes = new List<string>();

        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                var node = XElement.Load(file);

                if (!node.IsEmptyItem())
                {
                    var key = node.GetKey();

                    var folderName = Path.GetFileName(Path.GetDirectoryName(file));
                    var fileName = Path.GetFileName(file);

                    var filePath = $"\\{folderName}\\{fileName}";

                    if (_keys.TryGetValue(key, out string? value))
                    {
						clashes.Add($"<li>Clash [{filePath}] shares an id with [{value}]</li>");
					}
					else
                    {
						_keys[key] = filePath;
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
        var root = _fileService.GetAbsPath(_configService.GetWorkingFolder());

        if (_fileService.DirectoryExists(root) is false)
        {
            return new HealthCheckStatus("No uSync folder to check")
            {
                ResultType = StatusResultType.Success
            };
        }

        List<string> errors = [];

        foreach (var file in Directory.GetFiles(root, "*.config", SearchOption.AllDirectories))
        {
            try
            {
                var node = XElement.Load(file);
            }
            catch (Exception ex)
            {
                errors.Add($"<li>{Path.GetFileName(Path.GetDirectoryName(file))}\\{Path.GetFileName(file)} is invalid {ex.Message}</li>");
            }
        }

        if (errors.Count > 0)
        {
            return new HealthCheckStatus($"There are {errors.Count} Invalid .config files in the uSync folder")
            {
                Description = "<p>Some .config files are not valid xml, and will likely cause problems for a sync</p>" +
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
