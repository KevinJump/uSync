using System;
using System.Collections.Generic;
using System.Text;

namespace uSync.Core.Keys;

/// <summary>
///  Umbraco have decided the keys can't be changed anymore. 
/// </summary>
/// <remarks>
///  This is a very odd decision 🤷 as Keys where put in the database for this very reason (because you couldn't change internal table ids)
///  basically with a fixed guid, you just have a slower lookup key, because in sql guid lookups are slower than id ones, and all the other
///  benfits are gone. 
///  
///  It means we have to maintain a mapping of old keys to new keys, and this is that mapping.
///  because the keys are now fixed if a key is different but linked to anywhere (in a datatype,doctype, content or property)
///  then the previous solution of 'fixing' the key will no longer work. 
///  
///  so we have to keep a record of keys that come from the source that don't match thoese of the same items on the target site. 
///  and then when ever we encounter a key, we need to check and do a lookup to see if it needs to be mapped to the new key.
///  
///  we are going to try and cache this in memory for a sync, but because people can sync different bits at different times,
///  we need to keep a record of this in the database as well, so that if we sync content one day, and then sync doctypes
///  another day, the keys will still be mapped correctly.
/// </remarks>
public interface ISyncKeyMappingService
{
    Guid? FetchKey(Guid? key);  
    void AddKey(Guid? oldKey, Guid newKey);
    void DeleteKey(Guid? key);
}

internal class SyncKeyMappingService : ISyncKeyMappingService
{
    public Guid? FetchKey(Guid? key) => key;
    public void AddKey(Guid? oldKey, Guid newKey) { }
    public void DeleteKey(Guid? key) { }
}