using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace uSync.Extend.Example;


/// <summary>
///  this is the object you want to serialze, 
///  you can just use the object you alredy have,
///  or you can have something that represents only 
///  the bits you want to serialize.
/// </summary>
public class MyCustomObject
{
    public Guid Key { get; set; }
    public string Alias { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }

    [IgnoreDataMember]
    public string DontShowThis { get; set; } = string.Empty;
}
