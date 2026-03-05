namespace uSync.Core.Mapping;

/// <summary>
///  Defines a SyncMapper as something that returns null and not empty strings when a value isn't set. 
/// </summary>
public class NullableMapperAttribute : Attribute
{ }

/// <summary>
///  defines that a sync mapper requires a specific property editor to work.
///  This is used to ensure that mappers are only applied to properties that they can handle, 
///  and to prevent errors when a mapper is applied to a property that it cannot handle.
/// </summary>
public class RequiresPropertyEditorAttribute : Attribute
{
    public string Editor { get; }
    public RequiresPropertyEditorAttribute(string editor)
    {
        Editor = editor;
    }
}