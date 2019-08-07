using System.Collections.Generic;

using Umbraco.Core;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    /// <summary>
    ///  Mapper for anything that just stores a single or 
    ///  multiple Udis in a Comma Seperated list 
    /// </summary>
    /// <remarks>
    ///  These ids can be of any type the base class works 
    ///  out the dependcy order type based on the Udis 
    ///  
    ///  We are not supporting parial content imports, where 
    ///  content that this picker links to maynot be in the site
    ///  to do this we would need to map the UDI to something 
    ///  even more generic like a path. 
    /// </remarks>
    public class UdiPickerMapper : SyncValueMapperBase, ISyncMapper
    {
        public override string Name => "Content Picker Mapper";

        public override string[] Editors => new string[] {
            "Umbraco.ContentPicker",
            "Umbraco.MediaPicker",
            "Umbraco.MultiNodeTreePicker",
            "Umbraco.MemberPicker" };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var udiStrings = value.ToString().ToDelimitedList();
            return CreateDependencies(udiStrings, flags);
        }
    }
}
