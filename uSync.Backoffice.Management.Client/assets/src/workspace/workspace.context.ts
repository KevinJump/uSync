import { UmbArrayState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

import { uSyncActionRepository } from "..";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";
import { SyncActionGroup, SyncHandlerSummary, uSyncActionView } from "../api";

import { OpenAPI } from "../api";
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth'

/**
 * @exports 
 * @class uSyncWorkspaceActionContext
 * @description context for getting and seting up actions.
 */
export class uSyncWorkspaceContext extends UmbBaseController {

    #repository: uSyncActionRepository;

    constructor(host:UmbControllerHost) {
        super(host);

        this.provideContext(USYNC_CORE_CONTEXT_TOKEN, this);

        this.#repository = new uSyncActionRepository(this);

        this.consumeContext(UMB_AUTH_CONTEXT, (_auth) => {
            OpenAPI.TOKEN = () => _auth.getLatestToken();
            OpenAPI.WITH_CREDENTIALS = true;
            this.#loaded.setValue(true);
        });
    }

    #loaded = new UmbBooleanState(false);
    public readonly loaded = this.#loaded.asObservable();

    #actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
    public readonly actions = this.#actions.asObservable();

    #workingActions = new UmbArrayState<SyncHandlerSummary>([], (x) => x.name);
    public readonly currentAction = this.#workingActions.asObservable();

    #working = new UmbBooleanState(false);
    public readonly working = this.#working.asObservable();

    #completed = new UmbBooleanState(false);
    public readonly completed = this.#completed.asObservable();

    #results = new UmbArrayState<uSyncActionView>([], (x) => x.name);
    public readonly results = this.#results.asObservable();

    async getActions() {

        const { data } = await this.#repository.getActions();

        if (data) {           
            this.#actions.setValue(data);
        }
    }

    async performAction(group: string, key: string) {

        console.log("Perform Action:", group, key);

        this.#working.setValue(true);
        this.#completed.setValue(false);
        this.#results.setValue([]);

        var complete = false; 
        var id = '';
        var step: number = 0;

        do {

            const {data} = await this.#repository.performAction(id, group, key, step);

            if (data) {

                step++;

                console.log(data);

                let summary = data.status ?? [];
                
                this.#workingActions.setValue(summary);

                id = data.requestId;
                complete = data.complete;

                if (complete) {
                    this.#results.setValue(data?.actions ?? []);
                }

            }
            else {
                complete = true;
            }



        } while (!complete)
        

        this.#completed.setValue(true);
        this.#working.setValue(false);

    }
}

export default uSyncWorkspaceContext;

export const USYNC_CORE_CONTEXT_TOKEN = 
    new UmbContextToken<uSyncWorkspaceContext>(uSyncWorkspaceContext.name);