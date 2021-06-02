using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    public interface ISyncMapper
    {
        string Name { get; }
        string[] Editors { get; }

        bool IsMapper(PropertyType propertyType);

        string GetExportValue(object value, string editorAlias);
        string GetImportValue(string value, string editorAlias);

        IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags);
    }

    public interface ISyncMapperExtended : ISyncMapper
    {
        string GetExportValue(object value, SyncPropertyMapInfo propertyMapInfo);
        string GetImportValue(string value, SyncPropertyMapInfo propertyMapInfo);

        IEnumerable<uSyncDependency> GetDependencies(object value, SyncPropertyMapInfo propertyMapInfo, DependencyFlags flags);
    }

    public class SyncPropertyMapInfo
    {
        public string ContentTypeAlias { get; set; }
        public PropertyType PropertyType { get; set; }

        public string Culture { get; set; }
        public string Segment { get; set; }
    }
}
