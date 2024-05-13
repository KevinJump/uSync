# uSync file format change log. 

uSync saves items in '.config' files, which are xml representations of objects from the database (with some pre and post processing)

Very occasionaly we will need to change how something is stored within the uSync files. 

Changes are always backwards compatible (so old files will still import fine into usync) - but the actual changes in the file cause them to be registred as diffrent and show up as a change in the uSync UI.

When uSync performs a report/import it checks the version against a file in the root of the sync folder. if it is different you get a warning: 

![uSync format warning](warning.png)

Our recommendation is to perform a new export when the format version changes

## v14.0
- Datatypes
    - no longer store the database type in the datatype (this is driven by the property editor).
    - now include the UIEditorAlias value (datatypes have two editor types a Backend EditorUI and a Frontend property editor ui)
    - Config is not stored slightly diffrently as its split in Umbraco between frontend and backend values, this causes some things to be renamed/reordered.

## v10.1 

### Change to order of Json properties in datatypes. 
Sometimes the order of the properties inside a json string can change (if they change in the classes or something in the core). This makes checking the config of data types susceptible to these changes and
reporting false positives. 

this update ensures that when we serialize a datatype's config we put the properties in alphabetical order so there is less chance we call a change out when there isn't one.

## v8.17.0 / v9.0+

### Change to support tabs in Document types
Tab support in document types requires we now also store the alias, and tab type against a tab. for v8 these changes fail back to the name if they cannot find the alias, while in v9 the alias is always used. 

this change means that when using uSync to update from v8 to v9 - you will need to be running the latest uSync v8.10.2+ to get the tab values in the format that uSync 9 is expecting (if you are using tabs)

## v8.9.0 

### Change of media sync default (don't include file hash)
Change of the default for media handlers, so they no longer stamp the filehash into the config file. 
this means media syncs don't need to read the media files - so its quicker when your media is slow to get (e.g in the cloud or something).

uSync.Complete uses this feature - but forces it on by default - with normal uSync operation there is likely no need to have this set.

## v8.8.0

### Changes to Key used for Domain items (Culture and Hostname entries)
the internal key (guid) for domains isn't consistant so previously we used the ID of the domain turned into a guid - but as the ID can change between installs this isn't ideal.

As of v8. we use a guid generated from a hash of the domain name/language - this results in a new Key value in the file. 

**Impact:**
- False positive on change
- Performs reduntant import (Does not create duplicates)

## v8.7.0

### Changes to the Key used on well known member properties
the internal Key (GUID) used for member properties is not reliable, so we generate a key based on a hash of the property name. 

However through a reported issue #159 - we learnt that the GetHash is not deterministic and changes based on platform and processor - we updated the key generation algorithm for members to use a deterministic hash. 

**Impact:**
- False positive on change
- Performs reduntant import (Does not create duplicates)


