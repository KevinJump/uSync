

export enum ChangeDetailType {
    NO_CHANGE = 'NoChange',
    CREATE = 'Create',
    UPDATE = 'Update',
    DELETE = 'Delete',
    ERROR = 'Error',
    WARNING = 'Warning'
}

export enum ChangeType {
    NO_CHANGE = 'NoChange',
    CREATE = 'Create',
    IMPORT = 'Import',
    EXPORT = 'Export',
    UPDATE = 'Update',
    DELETE = 'Delete',
    WILL_CHANGE = 'WillChange',
    INFORMATION = 'Information',
    ROLLEDBACK = 'Rolledback',
    FAIL = 'Fail',
    IMPORT_FAIL = 'ImportFail',
    MISMATCH = 'Mismatch',
    PARENT_MISSING = 'ParentMissing',
    HIDDEN = 'Hidden',
    CLEAN = 'Clean',
    REMOVED = 'Removed'
}

export enum EventMessageTypeModel {
    DEFAULT = 'Default',
    INFO = 'Info',
    ERROR = 'Error',
    SUCCESS = 'Success',
    WARNING = 'Warning'
}

export type HandlerSettings = {
        enabled: boolean
actions: Array<string>
useFlatStructure: boolean
guidNames: boolean
failOnMissingParent: boolean
group: string
createClean: boolean
settings: Record<string, string>
    };

export enum HandlerStatus {
    PENDING = 'Pending',
    PROCESSING = 'Processing',
    COMPLETE = 'Complete',
    ERROR = 'Error'
}

export type NotificationHeaderModel = {
        message: string
category: string
type: EventMessageTypeModel
    };

export type PerformActionRequest = {
        requestId: string
action: string
stepNumber: number
options?: uSyncOptions | null
    };

export type PerformActionResponse = {
        requestId: string
status?: Array<SyncHandlerSummary> | null
actions?: Array<uSyncActionView> | null
complete: boolean
    };

export type SyncActionButton = {
        key: string
label: string
look: string
color: string
force: boolean
clean: boolean
children: Array<SyncActionButton>
    };

export type SyncActionGroup = {
        key: string
icon: string
groupName: string
buttons: Array<SyncActionButton>
    };

export type SyncHandlerSummary = {
        icon: string
name: string
status: HandlerStatus
changes: number
inError: boolean
    };

export type SyncLegacyCheckResponse = {
        hasLegacy: boolean
legacyFolder?: string | null
legacyTypes: Array<string>
    };

export type uSyncActionView = {
        key: string
name: string
itemType: string
change: ChangeType
success: boolean
details: Array<uSyncChange>
    };

export type uSyncAddonInfo = {
        version: string
    };

export type uSyncAddonSplash = Record<string, unknown>;

export type uSyncChange = {
        success: boolean
name: string
path: string
oldValue: string
newValue: string
change: ChangeDetailType
    };

export type uSyncHandlerSetSettings = {
        enabled: boolean
handlerGroups: Array<string>
disabledHandlers: Array<string>
handlerDefaults: HandlerSettings
handlers: Record<string, HandlerSettings>
isSelectable: boolean
    };

export type uSyncOptions = {
        clientId: string
force: boolean
clean: boolean
group: string
set: string
    };

export type uSyncSettings = {
        rootFolder: string
folders: Array<string>
legacyFolder: string
isRootSite: boolean
lockRoot: boolean
lockRootTypes: Array<string>
defaultSet: string
importAtStartup: string
exportAtStartup: string
exportOnSave: string
uiEnabledGroups: string
reportDebug: boolean
addOnPing: boolean
rebuildCacheOnCompletion: boolean
failOnMissingParent: boolean
failOnDuplicates: boolean
cacheFolderKeys: boolean
showVersionCheckWarning: boolean
customMappings: Record<string, string>
enableHistory: boolean
defaultExtension: string
importOnFirstBoot: boolean
firstBootGroup: string
disableDashboard: boolean
summaryDashboard: boolean
summaryLimit: number
hideAddOns: string
disableNotificationSuppression: boolean
backgroundNotifications: boolean
    };

export type ActionsData = {
        
        payloads: {
            PerformAction: {
                        requestBody?: PerformActionRequest
                        
                    };
        }
        
        
        responses: {
            GetActions: Array<SyncActionGroup>
                ,PerformAction: PerformActionResponse
                
        }
        
    }

export type MigrationsData = {
        
        
        responses: {
            CheckLegacy: SyncLegacyCheckResponse
                ,CopyLegacy: boolean
                ,IgnoreLegacy: boolean
                
        }
        
    }

export type SettingsData = {
        
        payloads: {
            GetHandlerSetSettings: {
                        id?: string
                        
                    };
        }
        
        
        responses: {
            GetAddOns: uSyncAddonInfo
                ,GetAddonSplash: uSyncAddonSplash
                ,GetHandlerSetSettings: uSyncHandlerSetSettings
                ,GetSettings: uSyncSettings
                
        }
        
    }