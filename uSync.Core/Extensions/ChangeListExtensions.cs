using System.Linq.Expressions;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core;

public static class ChangeListExtensions
{
    public static void AddNew(this List<uSyncChange> changes, string name, string value, string path)
        => changes.Add(uSyncChange.Create(path, name, value, true));

    [Obsolete("Use AddNew without success parameter instead, will be removed in v18")]
    public static void AddNew(this List<uSyncChange> changes, string name, string value, string path, bool success)
        => AddNew(changes, name, value, path);

    public static void AddIfUpdated<TObject>(this List<uSyncChange> changes, string name, TObject oldValue, TObject newValue, string path = "")
    {
        if ((newValue is null && oldValue is null) || (newValue?.Equals(oldValue) is true)) return;
        AddUpdate(changes, name, oldValue, newValue, path, true);
    }

    public static void AddUpdate<TObject>(this List<uSyncChange> changes, string name, TObject? oldValue, TObject? newValue, string path = "")
        => AddUpdate(changes, name, oldValue, newValue, path, true);

    public static void AddUpdate<TObject>(this List<uSyncChange> changes, string name, TObject? oldValue, TObject? newValue, string path, bool success)
        => AddUpdate(changes, name, oldValue?.ToString() ?? string.Empty, newValue?.ToString() ?? string.Empty, path, success);

    public static void AddUpdate(this List<uSyncChange> changes, string name, string? oldValue, string? newValue, string path = "")
        => AddUpdate(changes, name, oldValue, newValue, path, true);

    public static void AddUpdate(this List<uSyncChange> changes, string name, string? oldValue, string? newValue, string path, bool success)
        => changes.Add(uSyncChange.Update(path, name, oldValue, newValue, success));

    public static void AddWarning(this List<uSyncChange> changes, string path, string name, string warning)
        => changes.Add(uSyncChange.Warning(path, name, warning));

    public static void AddUpdateJson(this List<uSyncChange> changes, string name, object? oldValue, object? newValue, string path = "")
        => AddUpdateJson(changes, name, oldValue, newValue, path, true);

    public static void AddUpdateJson(this List<uSyncChange> changes, string name, object? oldValue, object? newValue, string path, bool success)
    {
        var oldJson = oldValue?.SerializeJsonString() ?? null;
        var newJson = newValue?.SerializeJsonString() ?? null;

        AddUpdate(changes, name, oldJson, newJson, path, success);
    }

    public static bool HasErrors(this List<uSyncChange> changes)
        => changes.Any(x => x.Change == ChangeDetailType.Error);

    public static bool HasWarning(this List<uSyncChange> changes)
        => changes.Any(x => x.Change == ChangeDetailType.Warning);


    /// <summary>
    /// If <paramref name="newValue"/> differs from the current property value,
    /// records a change and applies the setter.
    /// </summary>
    public static uSyncChange? ApplyIfChanged<TObject, TValue>(
        this TObject item,
        Expression<Func<TObject, TValue>> propertyExpr,
        TValue newValue,
        Action<TObject, TValue> setter,
        string path = "")
    {
        var getter = propertyExpr.Compile();
        var oldValue = getter(item);

        if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return null;

        var propName = ((MemberExpression)propertyExpr.Body).Member.Name;
        setter(item, newValue);
        
        return uSyncChange.Update(path, propName, oldValue?.ToString() ?? string.Empty, newValue?.ToString() ?? string.Empty, true);
    }
}
