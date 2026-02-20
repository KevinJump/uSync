using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Umbraco.Extensions;

namespace uSync.Core.Extensions;

/// <summary>
///  extension methods to get and set properties on objects, using reflection.
/// </summary>
public static class ObjectPropertyExtensions
{
    /// <summary>
    /// Determines whether the specified object contains a public property with the given name.
    /// </summary>
    /// <remarks>This method uses reflection to examine the object's type at runtime. Frequent use may have
    /// performance implications.</remarks>
    /// <param name="obj">The object to inspect for the presence of the specified property. Cannot be null.</param>
    /// <param name="propertyName">The name of the property to search for. The comparison is case-sensitive.</param>
    /// <returns>true if the object has a public property with the specified name; otherwise, false.</returns>
    public static bool HasProperty(this object obj, string propertyName)
        => obj.GetType().GetProperty(propertyName) != null;

    /// <summary>
    /// Retrieves the value of a specified property from the given object, or returns a default value if the property
    /// does not exist or cannot be accessed.
    /// </summary>
    /// <remarks>This method uses reflection to access the property. If the property is not found or cannot be
    /// accessed, no exception is thrown and the default value is returned.</remarks>
    /// <typeparam name="T">The type of the property value to retrieve.</typeparam>
    /// <param name="obj">The object from which to retrieve the property value. Cannot be null.</param>
    /// <param name="propertyName">The name of the property whose value is to be retrieved. The search is case-sensitive.</param>
    /// <param name="defaultValue">The value to return if the specified property does not exist or cannot be accessed.</param>
    /// <returns>The value of the specified property if it exists and can be accessed; otherwise, the provided default value.</returns>
    public static T GetPropertyValue<T>(this object obj, string propertyName, T defaultValue)
    {
        var propertyInfo = obj.GetType().GetProperty(propertyName);
        if (propertyInfo == null) return defaultValue;

        return GetPropertyAs<T>(propertyInfo, obj, defaultValue);
    }

    /// <summary>
    /// Sets the value of the specified property on the given object.
    /// </summary>
    /// <remarks>If the specified property does not exist on the object, the method returns the provided value
    /// without making any changes.</remarks>
    /// <typeparam name="T">The type of the value to assign to the property.</typeparam>
    /// <param name="obj">The object whose property value is to be set. This parameter cannot be null.</param>
    /// <param name="propertyName">The name of the property to set. This must correspond to a public property on the object.</param>
    /// <param name="value">The value to assign to the specified property.</param>
    /// <returns>The value that was assigned to the property.</returns>
    public static T SetPropertyValue<T>(this object obj, string propertyName, T value)
    {
        var propertyInfo = obj.GetType().GetProperty(propertyName);
        if (propertyInfo == null) return value;
        propertyInfo.SetValue(obj, value);
        return value;
    }

    /// <summary>
    /// Retrieves the value of the specified property and attempts to convert it to the specified type.
    /// </summary>
    /// <remarks>If the property is not found or the conversion fails, the method returns the default value
    /// provided.</remarks>
    /// <typeparam name="TValue">The type to which the property value is converted.</typeparam>
    /// <param name="info">The PropertyInfo object representing the property to retrieve the value from.</param>
    /// <param name="property">The object instance from which to retrieve the property value.</param>
    /// <param name="defaultValue">The value to return if the property is null or cannot be converted to the specified type.</param>
    /// <returns>The converted value of the property if successful; otherwise, the specified default value.</returns>
    private static TValue GetPropertyAs<TValue>(PropertyInfo info, object property, TValue defaultValue)
    {
        if (info == null) return defaultValue;

        var value = info.GetValue(property);
        if (value == null) return defaultValue;

        var result = value.TryConvertTo<TValue>();
        if (result.Success)
            return result.Result ?? defaultValue;

        return defaultValue;

    }
}
