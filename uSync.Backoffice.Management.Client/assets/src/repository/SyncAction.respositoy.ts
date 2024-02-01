import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { uSyncActionDataSource } from "./sources/SyncAction.source";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";

/**
 * @export
 * @class uSyncActionRepository
 * @description repository for all things actions.
 */
export class uSyncActionRepository extends UmbBaseController {
    #actionDataSource: uSyncActionDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#actionDataSource = new uSyncActionDataSource(this);
    }

    /**
     * @method getActions
     * @description get the list of possible actions from the server
     * @returns Promise
     */
    async getActions() {
        return this.#actionDataSource.getActions();
    }

    /**
     * 
     * @param id - id for this run (each run has its own unquie id)
     * @param group - the group (e.g settings, content, all)
     * @param action - the action ('report', 'import', 'export')
     * @param step - the step number (this increments each call)
     * @param clientId  - the signalR client id, so we can send updates.
     * @returns PeformActionResponse
     */
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