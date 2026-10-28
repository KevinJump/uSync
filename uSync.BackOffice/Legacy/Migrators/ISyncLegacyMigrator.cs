using System;
using System.Xml.Linq;

namespace uSync.BackOffice.Legacy.Migrators;

/// <summary>
///  moves a deprecated (or since split) editor onto its replacement, as legacy files are copied
///  into the current uSync folder.
/// </summary>
/// <remarks>
///  migrators run on the copied files, so the import that follows is a normal import of files
///  that already describe the new editors.
/// </remarks>
public interface ISyncLegacyMigrator
{
    /// <summary>
    ///  unique id, used by the UI to say which migrators to run.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///  short name to show the user.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///  short description of what the migrator changes.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///  anything the user should know before running it (limits, side effects).
    /// </summary>
    string? Notice { get; }

    /// <summary>
    ///  does this migrator apply to the data type, in files written by the given major version.
    /// </summary>
    bool AppliesTo(XElement dataType, int folderMajorVersion);

    /// <summary>
    ///  rewrite the data type node (editor alias, editor UI alias, config) in place.
    /// </summary>
    /// <returns>what changed, or null if the data type was left alone.</returns>
    SyncLegacyDataTypeMigration? MigrateDataType(XElement dataType);

    /// <summary>
    ///  true if the new editor stores its values differently, and values need rewriting too.
    /// </summary>
    bool MigratesValues => false;

    /// <summary>
    ///  rewrite a stored property value for a migrated data type.
    /// </summary>
    string MigrateValue(string value, SyncLegacyDataTypeMigration migration) => value;
}

/// <summary>
///  details of a data type a migrator has moved onto a new editor.
/// </summary>
public record SyncLegacyDataTypeMigration(Guid DataTypeKey, string OriginalEditorAlias, string NewEditorAlias, ISyncLegacyMigrator Migrator);

/// <summary>
///  a migrator that is available for a legacy folder.
/// </summary>
public record SyncLegacyMigrationInfo(string Id, string Name, string Description, string? Notice, int Count);
