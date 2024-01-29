import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { DataSourceResponse } from "@umbraco-cms/backoffice/repository";
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';
import { ActionsResource, SyncActionGroup } from "../../api";


export interface SyncActionDataSource {
    getActions() : Promise<DataSourceResponse<unknown>>;
}

export class uSyncActionDataSource implements SyncActionDataSource {

    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getActions(): Promise<DataSourceResponse<Array<SyncActionGroup>>> {
        return await tryExecuteAndNotify(this.#host, ActionsResource.getUmbracoManagementApiV1USyncActions());
    }

    async getTime(): Promise<DataSourceResponse<string>> {
        return await tryExecuteAndNotify(this.#host, ActionsResource.getUmbracoManagementApiV1USyncTime());
    }
}



