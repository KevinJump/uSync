//using Newtonsoft.Json;
//using Newtonsoft.Json.Serialization;

//namespace uSync.Core.Json;

///// <summary>
/////  for some consistancy this resolver will serialize json 
/////  alphabetically, which will help comparing changes,
/////  because sometimes properties move around inside datatype 
/////  configs. 
///// </summary>
//internal class uSyncContractResolver : DefaultContractResolver
//{
//    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
//    {
//        return base.CreateProperties(type, memberSerialization)
//            .OrderBy(p => p.PropertyName).ToList();
//    }
//}
