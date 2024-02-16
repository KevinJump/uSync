/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { SyncHandlerSummary } from './SyncHandlerSummary';
import type { uSyncActionView } from './uSyncActionView';

export type PerformActionResponse = {
    requestId: string;
    status?: Array<SyncHandlerSummary> | null;
    actions?: Array<uSyncActionView> | null;
    complete: boolean;
};
