/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type uSyncSettings = {
    rootFolder: string;
    folders: Array<string>;
    legacyFolder: string;
    isRootSite: boolean;
    lockRoot: boolean;
    lockRootTypes: Array<string>;
    defaultSet: string;
    importAtStartup: string;
    exportAtStartup: string;
    exportOnSave: string;
    uiEnabledGroups: string;
    reportDebug: boolean;
    addOnPing: boolean;
    rebuildCacheOnCompletion: boolean;
    failOnMissingParent: boolean;
    failOnDuplicates: boolean;
    cacheFolderKeys: boolean;
    showVersionCheckWarning: boolean;
    customMappings: Record<string, string>;
    /**
     * @deprecated
     */
    signalRRoot: string;
    enableHistory: boolean;
    defaultExtension: string;
    importOnFirstBoot: boolean;
    firstBootGroup: string;
    disableDashboard: boolean;
    summaryDashboard: boolean;
    summaryLimit: number;
    hideAddOns: string;
    disableNotificationSuppression: boolean;
    backgroundNotifications: boolean;
};
