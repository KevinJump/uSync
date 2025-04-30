using System.IO;
using System.Reflection;
using System;
using System.Diagnostics;
using Umbraco.Cms.Core.Semver;
using Umbraco.Extensions;

namespace uSync.BackOffice.Extensions;

/// <summary>
///  extensions for working with assemblies
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    ///  Gets the product version from the file info for an assembly.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static SemVersion GetAssemblyProductVersion(this Assembly assembly)
    {
        var fileInfo = new FileInfo(new Uri(assembly.Location).LocalPath);
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);
        return SemVersion.Parse(fileVersionInfo.ProductVersion ?? assembly.GetName()?.Version?.ToString(3) ?? "15.0.0");

    }
}
