/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { HandlerSettings } from './HandlerSettings';

export type uSyncHandlerSetSettings = {
    enabled: boolean;
    handlerGroups: Array<string>;
    disabledHandlers: Array<string>;
    handlerDefaults: HandlerSettings;
    handlers: Record<string, HandlerSettings>;
    isSelectable: boolean;
};
