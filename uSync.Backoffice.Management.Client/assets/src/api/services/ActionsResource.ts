/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PerformActionRequest } from '../models/PerformActionRequest';
import type { PerformActionResponse } from '../models/PerformActionResponse';
import type { SyncActionGroup } from '../models/SyncActionGroup';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class ActionsResource {

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static getActions(): CancelablePromise<Array<SyncActionGroup>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/umbraco/usync/api/v1/Actions',
        });
    }

    /**
     * @returns any Success
     * @throws ApiError
     */
    public static performAction({
requestBody,
}: {
requestBody?: PerformActionRequest,
}): CancelablePromise<PerformActionResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/umbraco/usync/api/v1/Perform',
            body: requestBody,
            mediaType: 'application/json',
        });
    }

}
