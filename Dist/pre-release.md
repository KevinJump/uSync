# uSync checklist 

This is in addtion to any automated testing/etc (although it would be nice to automate this to). checklist of pre-release tests - to make sure things work as we expect them to in the 'real' world. 

## Basic setup

### New Site
- [ ] Clean install doesn't break anything
- [ ] Installed before Umbraco is setup doesn't break anything 
- [ ] Export works
- [ ] Report doesn't detect any changes
- [ ] Import doesn't detect any changes 
- [ ] Full Import imports everything 
- [ ] Restart Site 
  - [ ] Report after restart doesn't detect any changes 
  - [ ] Import after restart doesn't detect any changes 

### Upgrade 
- [ ] Upgrading site from previous doesn't break site
- [ ] All files are updated
- [ ] javascript/css cache is broken if needed
- [ ] Report has no changes (point release)
- [ ] Import doesn't import anything
- [ ] Full Import still works
- [ ] Export works. 

### Site clone 
- [ ] New empty site with uSync installed (no starter kit)
- [ ] Copy of folders (App_Plugins/css/scripts/views/media/uSync)
	- [ ] Import imports everything (except for contact - missing dll)
	- [ ] report only reports items dependeny on maps.
	- [ ] Copy the map dll from bin
	- [ ] Import imports the missing item 
	- [ ] Report details no changes between sites 
	
### Deletion
- [ ] Removal of Datatype 
- [ ] Removal of DocType
- [ ] Removal of Content
- [ ] Removal of Media 

### Creation
- [ ] New Datatype copied as expected
- [ ] New Doctype copied as expected
- [ ] New Content Item copied as expected
- [ ] New Media item copied as expected 

### Updates
- [ ] update to Datatype copied as expected
- [ ] update to Doctype copied as expected
- [ ] update to Content Item copied as expected
- [ ] update to Media item copied as expected 

### Cultures (create welsh homepage on source)
- [ ] Language is created on target
- [ ] Content for language is created on target 
- [ ] Content is published as expected

### Scheduling 
- [ ] Scheduled content is copied over to the new site
- [ ] Scheduled release in the past results in published item
- [ ] Scheduled expire in the past results in unpublished item
- [ ] Scheule of one language works as expected

### uSync.Complete (Backwards compatability)
- [ ] Confirm that the current uSync.Complete checklist works with this version 