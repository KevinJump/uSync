/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { HandlerSettings } from './HandlerSettings';

export type uSyncHandlerSetSettings = {
    enabled: boolean;
    handlerGroups?: Array<string> | null;
    disabledHandlers?: Array<string> | null;
    handlerDefaults?: HandlerSettings | null;
    handlers?: Record<string, HandlerSettings> | null;
    isSelectable: boolean;
};
