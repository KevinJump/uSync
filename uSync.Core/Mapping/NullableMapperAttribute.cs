using System;

namespace uSync.Core.Mapping;

/// <summary>
///  Defines a SyncMapper as something that returns null and not empty strings when a value isn't set. 
/// </summary>
public class NullableMapperAttribute : Attribute
{ }
