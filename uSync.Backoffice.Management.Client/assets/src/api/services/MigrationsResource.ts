/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { SyncLegacyCheckResponse } from '../models/SyncLegacyCheckResponse';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class MigrationsResource {

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static checkLegacy(): CancelablePromise<SyncLegacyCheckResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/usync/api/v1/CheckLegacy',
        });
    }

}
