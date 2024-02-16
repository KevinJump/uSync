/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChangeType } from './ChangeType';
import type { uSyncChange } from './uSyncChange';

export type uSyncActionView = {
    name: string;
    itemType: string;
    change: ChangeType;
    success: boolean;
    details: Array<uSyncChange>;
};
