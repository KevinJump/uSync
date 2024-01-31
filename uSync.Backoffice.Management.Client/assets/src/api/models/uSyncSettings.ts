/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type uSyncSettings = {
    rootFolder?: string | null;
    folders?: Array<string> | null;
    isRootSite: boolean;
    lockRoot: boolean;
    lockRootTypes?: Array<string> | null;
    defaultSet?: string | null;
    importAtStartup?: string | null;
    exportAtStartup?: string | null;
    exportOnSave?: string | null;
    uiEnabledGroups?: string | null;
    reportDebug: boolean;
    addOnPing: boolean;
    rebuildCacheOnCompletion: boolean;
    failOnMissingParent: boolean;
    cacheFolderKeys: boolean;
    showVersionCheckWarning: boolean;
    customMappings?: Record<string, string> | null;
    /**
     * @deprecated
     */
    signalRRoot?: string | null;
    enableHistory: boolean;
    defaultExtension?: string | null;
    importOnFirstBoot: boolean;
    firstBootGroup?: string | null;
    disableDashboard: boolean;
    summaryDashboard: boolean;
    summaryLimit: number;
    hideAddOns?: string | null;
    disableNotificationSuppression: boolean;
};
