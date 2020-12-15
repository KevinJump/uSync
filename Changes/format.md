# uSync file format change log. 

uSync saves items in '.config' files, which are xml representations of objects from the database (with some pre and post processing)

Very occasionaly we will need to change how something is stored within the uSync files. 

Changes are always backwards compatible (so old files will still import fine into usync) - but the actual changes in the file cause them to be registred as diffrent and show up as a change in the uSync UI.

When uSync performs a report/import it checks the version against a file in the root of the sync folder. if it is diffrent you get a warning: 

![uSync format warning](warning.png)

Our recommendation is to perform a new export when the format version changes

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


