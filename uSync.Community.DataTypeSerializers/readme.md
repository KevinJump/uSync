## uSync.Community.DataTypeSerializers

Additional Serializers for datatypes in uSync.

If you are not also syncing content between your Umbraco sites, then you might need to use the DatatypeSerializers.

The DataTypeSerializers will take any references to content or media in a datatype (like a start node for a picker) and make them portable. 

during export the key values are converted to paths from the content, and when you re-import those paths are turned back into the key values.

### Only if you don't sync content.
if you are syncing content this isn't required becaus the content keys will also be synced. 