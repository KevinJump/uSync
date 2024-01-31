import { UmbArrayState, UmbBooleanState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

import { uSyncActionRepository } from "..";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBaseController } from "@umbraco-cms/backoffice/class-api";
import { ActionInfo, SyncActionGroup } from "../api";

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

    #workingActions = new UmbArrayState<ActionInfo>([], (x) => x.actionName);
    public readonly currentAction = this.#workingActions.asObservable();

    #working = new UmbBooleanState(false);
    public readonly working = this.#working.asObservable();

    #completed = new UmbBooleanState(false);
    public readonly completed = this.#completed.asObservable();

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


    public async performAction(group: string, key: string) {

        console.log("Perform Action:", group, key);

        this.#working.setValue(true);
        this.#completed.setValue(false);

        var complete = false; 
        var id = '';
        var step: number = 0;

        do {

            const {data} = await this.#repository.performAction(id, group, key, step);

            if (data) {

                step++;

                console.log(data);

                
                this.#workingActions.setValue(data.actionInfo);


                id = data.requestId;
                complete = data.completed;

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