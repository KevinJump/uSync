/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { SyncActionGroup } from '../models/SyncActionGroup';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class ActionsResource {

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static getUmbracoManagementApiV1USyncActions(): CancelablePromise<Array<SyncActionGroup>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/management/api/v1/uSync/actions',
        });
    }

    /**
     * @returns string Success
     * @throws ApiError
     */
    public static getUmbracoManagementApiV1USyncTime(): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/management/api/v1/uSync/time',
        });
    }

}
