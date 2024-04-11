export default {
    uSync: {
        name: 'uSync',
        banner: 'uSync all the things',

        Report: 'Report',
        Import: 'Import',
        Export: 'Export',
        ImportForce: 'Import (Force)',
        ExportClean: 'Export (Clean)'

    },
    uSyncSettings: {
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
        foldersDesc: 'Folders uSync will look for files, (items are normally saved into last folder in the list)',

        rootSite: 'Root Site',
        rootSiteDesc: 'Is this site a root for other sites.',

        rootLocked: 'Root Locked',
        rootLockedDesc: 'Are changes for items that are from the root site locked?',

        help: 'Settings are controlled via the appsettings.json file. <a href="https://docs.jumoo.co.uk/usync/uSync/reference/config" target="_blank" rel="noopener">see our docs</a>'
    }
};