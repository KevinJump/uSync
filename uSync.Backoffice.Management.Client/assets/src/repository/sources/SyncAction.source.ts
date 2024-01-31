import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { DataSourceResponse } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';
import { ActionsResource, PerformActionRequestModel, PerformActionResponse, SyncActionGroup } from "../../api";


export interface SyncActionDataSource {
    getActions() : Promise<DataSourceResponse<unknown>>;
}

export class uSyncActionDataSource implements SyncActionDataSource {

    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getActions(): Promise<DataSourceResponse<Array<SyncActionGroup>>> {
        return await tryExecuteAndNotify(this.#host, ActionsResource.getActions());
    }

    async getTime(): Promise<DataSourceResponse<string>> {
        return await tryExecuteAndNotify(this.#host, ActionsResource.getTime());
    }

    async performAction(request : PerformActionRequestModel): Promise<DataSourceResponse<PerformActionResponse>> {
        return await tryExecuteAndNotify(this.#host, ActionsResource.performAction({
            requestBody: request
        }));
    }
}



