/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { uSyncAddonInfo } from '../models/uSyncAddonInfo';
import type { uSyncAddonSplash } from '../models/uSyncAddonSplash';
import type { uSyncSettings } from '../models/uSyncSettings';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class SettingsResource {

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static getAddOns(): CancelablePromise<uSyncAddonInfo> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/usync/api/v1/AddOns',
        });
    }

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static getAddonSplash(): CancelablePromise<uSyncAddonSplash> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/usync/api/v1/AddOnSplash',
        });
    }

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static getSettings(): CancelablePromise<uSyncSettings> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/usync/api/v1/Settings',
        });
    }

}
