import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { uSyncActionDataSource } from "./sources/SyncAction.source";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";

export class uSyncActionRepository extends UmbBaseController {
    #actionDataSource: uSyncActionDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#actionDataSource = new uSyncActionDataSource(this);
    }

    async getActions() {
        return this.#actionDataSource.getActions();
    }

    async performAction(id: string, group: string, action: string, step: number,
        clientId: string) {

        return this.#actionDataSource.performAction(
            {
                requestId : id, 
                action: action,
                options: {
                    group : group,
                    force : true,
                    clean : false,
                    clientId : clientId
                },
                stepNumber: step
            }
        );

    }
}