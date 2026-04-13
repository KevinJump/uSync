using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Semver;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.Core;
using uSync.Core.Versions;

namespace uSync.BackOffice.Services;

internal class SyncVersionFileService : ISyncVersionFileService
{
    private readonly ISyncFileService _syncFileService;
    private readonly ISyncConfigService _uSyncConfig;
    private readonly ILogger<SyncVersionFileService> _logger;
    private readonly ISyncImageUpdateHelper _syncImageUpdateHelper;

    public SyncVersionFileService(
        ISyncFileService syncFileService,
        ISyncConfigService uSyncConfig,
        ILogger<SyncVersionFileService> logger,
        ISyncImageUpdateHelper syncImageUpdateHelper)
    {
        _syncFileService = syncFileService;
        _uSyncConfig = uSyncConfig;
        _logger = logger;
        _syncImageUpdateHelper = syncImageUpdateHelper;
    }

    public async Task WriteVersionFileAsync(string folder)
    {
        try
        {
            var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");
            var versionNode = new XElement("uSync",
                new XAttribute("version", typeof(uSync).Assembly.GetName()?.Version?.ToString() ?? "15.0.0"),
                new XAttribute("format", Core.uSyncConstants.FormatVersion),
                new XAttribute("hmac", _syncImageUpdateHelper.GetImageHmacString(Core.uSyncConstants.FormatVersion))
            );

            _syncFileService.CreateFoldersForFile(versionFile);
            await _syncFileService.SaveXElementAsync(versionNode, versionFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Issue saving the usync.config file in the root of {folder}", folder);
        }
    }

    public async Task<SyncFileVersionCheckResult> GetSyncFileInfo(string folder)
    {
        var result = new SyncFileVersionCheckResult
        {
            IsCurrent = true,
            HmacMatch = true
        };

        var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");

        if (!_syncFileService.FileExists(versionFile))
        {
            result.IsCurrent = false;
            result.HmacMatch = false;
        }
        else
        {
            try
            {
                var node = await _syncFileService.LoadXElementAsync(versionFile);
                result.FormatVersion = GetFormatVersionFromFile(node);
                result.IsCurrent = IsCurrentFormatVersion(result.FormatVersion);
                result.HmacMatch = HmacValuesMatch(node);
            }
            catch
            {

            }
        }

        return result;
    }

    private static string GetFormatVersionFromFile(XElement node)
        => node.Attribute("format").ValueOrDefault("");

    private static bool IsCurrentFormatVersion(string formatVersion)
    {
        if (string.IsNullOrWhiteSpace(formatVersion))
        {
            return false;
        }

        if (formatVersion.InvariantEquals(Core.uSyncConstants.FormatVersion))
        {
            return true;
        }

        var expectedVersion = SemVersion.Parse(Core.uSyncConstants.FormatVersion);
        if (SemVersion.TryParse(formatVersion, out SemVersion? current) && current is not null)
        {
            return current.CompareTo(expectedVersion) >= 0;
        }

        return false;
    }

    private bool HmacValuesMatch(XElement node)
    {
        var hmac = node.Attribute("hmac").ValueOrDefault("");
        var format = node.Attribute("format").ValueOrDefault("");
        var expectedHmac = _syncImageUpdateHelper.GetImageHmacString(format);
        return hmac.InvariantEquals(expectedHmac);
    }
}

public class SyncFileVersionCheckResult
{
    public bool IsCurrent { get; set; }
    public string? FormatVersion { get; set; }
    public bool HmacMatch { get; set; }
}
