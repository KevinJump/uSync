import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { uSyncActionDataSource } from "./sources/SyncAction.source";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";

export class uSyncActionRepository extends UmbBaseController {
    #host: UmbControllerHost;
    #actionDataSource: uSyncActionDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#host = host;
        this.#actionDataSource = new uSyncActionDataSource(this);
        console.log('respository init');
    }

    async getActions() {
        return this.#actionDataSource.getActions();
    }

    async getTime() {
        return this.#actionDataSource.getTime();
    }
}