﻿using System.Xml.Linq;

using Umbraco.Extensions;

namespace uSync.Core.Serialization;

/// <summary>
///  options class that can be passed to a serialize/deserialize method.
/// </summary>
public class SyncSerializerOptions
{
    // the user who is doing the serialization 
    public int UserId = -1;

    public SyncSerializerOptions() { }

    public SyncSerializerOptions(SerializerFlags flags)
    {
        this.Flags = flags;
    }

    public SyncSerializerOptions(Dictionary<string, string> settings)
    {
        this.Settings = settings != null ? new Dictionary<string, string>(settings) : new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    }

    public SyncSerializerOptions(SerializerFlags flags, Dictionary<string, string> settings)
        : this(settings)
    {
        this.Flags = flags;
    }

    public SyncSerializerOptions(SerializerFlags flags, Dictionary<string, string> settings, int userId)
        : this(flags, settings)
    {
        UserId = userId;
    }

    /// <summary>
    ///  Only create item if it doesn't already exist.
    /// </summary>
    public bool CreateOnly { get; set; }

    /// <summary>
    ///  Serializer flags, turn things like Don't Save, FailWhenParent is missing on
    /// </summary>
    /// <remarks>
    ///  this is now private - we might phase it out for the options instead.
    /// </remarks>
    public SerializerFlags Flags { get; internal set; }

    /// <summary>
    ///  Parameterized options, custom for each handler
    /// </summary>
    public Dictionary<string, string> Settings { get; internal set; } = [];

    /// <summary>
    ///  flag properties, we can move this away from flags if we want to.
    /// </summary>
    public bool Force => Flags.HasFlag(SerializerFlags.Force);

    // public bool DoNotSave => Flags.HasFlag(SerializerFlags.DoNotSave);

    public bool FailOnMissingParent => Flags.HasFlag(SerializerFlags.FailMissingParent);

    public bool OnePass => Flags.HasFlag(SerializerFlags.OnePass);

    public TResult GetSetting<TResult>(string key, TResult defaultValue)
    {
        if (this.Settings != null && this.Settings.ContainsKey(key))
        {
            var attempt = this.Settings[key].TryConvertTo<TResult>();
            if (attempt.Success && attempt.Result is not null)
                return attempt.Result;
        }

        return defaultValue;
    }

    /// <summary>
    ///  Get the cultures defined in the settings.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetCultures() => GetSetting(uSyncConstants.CultureKey, string.Empty).ToDelimitedList();

    /// <summary>
    ///  fail import if for some reasons we have warnings. 
    /// </summary>
    public bool FailOnWarnings() => GetSetting<bool>("FailOnWarnings", false);


    /// <summary>
    ///  Gets the cultures that can be de-serialized from this node.
    /// </summary>
    /// <remarks>
    ///  If a node has 'Cultures' set on the top node, then its only a partial sync
    ///  so we need to treat all the functions like this is set on the handler..
    /// </remarks>
    public IList<string> GetDeserializedCultures(XElement node)
    {
        var nodeCultures = node.GetCultures();
        if (!string.IsNullOrEmpty(nodeCultures)) return nodeCultures.ToDelimitedList();
        return GetCultures();
    }

    public IList<string> GetSegments()
        => GetSetting(uSyncConstants.SegmentKey, string.Empty).ToDelimitedList();

    /// <summary>
    ///  merge any new settings into the settings collection.
    /// </summary>
    public void MergeSettings(Dictionary<string, string>? newSettings)
    {
        if (Settings is null)
        {
            Settings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        if (newSettings is not null)
        {
            foreach (var kvp in newSettings)
            {
                Settings[kvp.Key] = kvp.Value;
            }
        }
    }
}
