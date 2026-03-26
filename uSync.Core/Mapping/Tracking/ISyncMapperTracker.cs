using System;
using System.Collections.Generic;
using System.Text;

namespace uSync.Core.Mapping.Tracking;

/// <summary>
///  tracking for when editors might change. 
/// </summary>
/// <remarks>
///  A tracker mapper can return additional ISyncMappers for an editor Alias
///  typically we do this when migrations has tracked that a new editorAlias 
///  was once an a different editor alias.
/// </remarks>
public interface ISyncMapperTracker
{
    Task<IEnumerable<ISyncMapper>> GetTrackingMappers(string editorAlias);
}
