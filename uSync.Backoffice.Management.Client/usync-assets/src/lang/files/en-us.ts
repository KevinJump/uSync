export default {
	uSync: {
		name: 'uSync',
		banner: 'uSync all the things',

		Report: 'Report',
		Import: 'Import',
		Export: 'Export',
		ImportForce: 'Import (Force)',
		ExportClean: 'Export (Clean)',
		ExportFile: 'Export to File',
		ImportFile: 'Import from File',

		noChange: 'Nothing has changed',
		showAll: 'Show all items',

		detailHeadline: 'Detected Changes',
		detailHeader: 'Things that are different',

		changeAction: 'Action',
		changeItem: 'Item',
		changeDiffrence: 'Difference',
		changeCreate: 'This item is being created',

		success: 'Success',
		change: 'Change',
		changeType: 'Type',
		changeName: 'Name',
		changeDetail: 'Detail',
		changeHeading: 'Results',
		changeCount: '{1}/{0} changes',
		noChangeCount: '0/{0} changes',

		legacyInfo: `<p>uSync has found a legacy uSync folder at <strong>%0%</strong>.<br />
						Its likely that the content in it will need coverting in someway</p>`,

		legacyObsolete: `<h4>Obsolete DataTypes</h4>
				<ul>%0%</ul>
				<p>You can convert these DataTypes using
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br />
					<em>(In the full uSync release conversion will happen here.)</em></p>`,

		legacyCopy: `<h4>Copy to uSync/v14</h4>
					<p>You can copy your %0% folder to the ~/uSync/v14 folder<br />and run an import.</p>
					<p>If nothing needs converting, then everything should import.</p>
					<p><strong>Remove or rename the %0% folder to prevent this	popup</strong></p>`,

		legacyBanner:
			'This site contains files from a previous version of uSync, view the details in the legacy tab.',

		legacyCopyTitle: 'Overwrite v%0% files',
		legacyCopyContent:
			'Are you sure you want to overwrite the contents of the %0% folder with the legacy uSync folder files?',

		legacyIgnoreTitle: 'Ignore legacy files',
		legacyIgnoreContent:
			'Are you sure you want to ignore the files in the legacy uSync folder?',

		errorHeader:
			'This item encountered an error during the process, the details are below:',

		uploadIntro: 'Select a zip file containing uSync files that you want to upload',
		uploadSuccess: 'The files have been uploaded and extracted to the uSync folder',
		uploadError: 'There was an error uploading the files',

		ILanguage: 'Language',
		IDictionaryItem: 'Dictionary Items',
		IDataType: 'DataTypes',
		ITemplate: 'Templates',
		IContentType: 'Content Types',
		IMediaType: 'Media Types',
		IMemberType: 'Member Types',
		IContent: 'Content',
		IMedia: 'Media',
		IDomain: 'Domains',
		IWebhook: 'Webhooks',
		IRelationType: 'Relation Types',
		MediaFile: 'Media Files',
		XElement: 'Other',
	},
	USyncSettings: {
		settings: 'uSync Settings',
		filesAndFolders: 'File and folders',
		handlerDefaults: 'Handler defaults',

		importAtStartup: 'Import at startup',
		importAtStartupDesc: 'Run an import of files from the disk when Umbraco starts',

		exportAtStartup: 'Export at stattup',
		exportAtStartupDesc: 'Export the Umbraco settings when the site starts up',

		exportOnSave: 'Export on save',
		exportOnSaveDesc: 'Generate uSync files when items are saved',

		uiEnabledGroups: 'UI Enabled groups',
		uiEnabledGroupsDesc: 'Handler groups that can be seen/used on the dashboard',

		failOnMissingParent: 'Fail on missing parent',
		failOnMissingParentDesc: 'Fail on missing parent',

		flatStructure: 'Flat structure',
		flatStructureDesc: 'All items of a type are stored in a flat folder structure',

		guidNames: 'Use guids for filenames',
		guidNamesDesc: 'Use the GUID of an item as the filename',

		handlerGroups: 'Handler groups',
		handlerGroupsDesc: 'Groups to limit handler set to',

		disabledHandlers: 'Disabled Handlers',
		disabledHandlersDesc: 'Handlers explicitly disabled for this handler set',

		folders: 'Folders',
		foldersDesc:
			'Folders uSync will look for files, (items are normally saved into last folder in the list)',

		rootSite: 'Root Site',
		rootSiteDesc: 'Is this site a root for other sites.',

		rootLocked: 'Root Locked',
		rootLockedDesc: 'Are changes for items that are from the root site locked?',

		help: 'Settings are controlled via the appsettings.json file. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">see our docs</a>',

		bootSettings: 'First boot 🥾 settings',

		firstBoot: 'Import on First boot',
		firstBootDesc: 'Run the import process on first boot of the site',

		firstBootGroup: 'First boot groups',
		firstBootGroupDesc: 'The groups to run on first boot',
	},
};
