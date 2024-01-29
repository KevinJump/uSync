import { UmbArrayState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

import { uSyncActionRepository } from "../..";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";
import { SyncActionGroup } from "../../api";

/**
 * @exports 
 * @class uSyncWorkspaceActionContext
 * @description context for getting and seting up actions.
 */
export class uSyncWorkspaceActionContext extends UmbBaseController {

    #repository: uSyncActionRepository;

    constructor(host:UmbControllerHost) {
        super(host);

        this.provideContext(USYNC_ACTION_CONTEXT_TOKEN, this);
        this.#repository = new uSyncActionRepository(this);
    }

    #actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
    public readonly actions = this.#actions.asObservable();

    async getActions() {

        const { data } = await this.#repository.getActions();

        if (data) {           
            this.#actions.setValue(data);
        }
    }

    async getTime() {
        const {data} = await this.#repository.getTime();

        if (data) {
            console.log(data);
        }
    }


    public performAction(group: string, key: string) {

        console.log("Perform Action:", group, key);

    }
}

export default uSyncWorkspaceActionContext;

export const USYNC_ACTION_CONTEXT_TOKEN = 
    new UmbContextToken<uSyncWorkspaceActionContext>(uSyncWorkspaceActionContext.name);