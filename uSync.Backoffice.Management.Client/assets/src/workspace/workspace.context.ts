import { UmbArrayState, UmbBooleanState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

import { uSyncActionRepository } from "..";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { SyncActionGroup, SyncHandlerSummary, uSyncActionView, uSyncHandlerSetSettings, uSyncSettings } from "../api";

import { OpenAPI } from "../api";
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth'
import uSyncSignalRContext, { USYNC_SIGNALR_CONTEXT_TOKEN } from "../signalr/signalr.context";
import { UMB_WORKSPACE_CONTEXT, type UmbWorkspaceContextInterface } from "@umbraco-cms/backoffice/workspace";
import { uSyncConstants } from "../constants";
import { uSyncIconRegistry } from "../icons";

/**
 * @exports 
 * @class uSyncWorkspaceActionContext
 * @description context for getting and seting up actions.
 */
export class uSyncWorkspaceContext extends UmbControllerBase
    implements UmbWorkspaceContextInterface {
    public readonly workspaceAlias: string = uSyncConstants.workspace.alias;

    getEntityType(): string {
        return uSyncConstants.workspace.rootElement;
    }
    getEntityId(): string | undefined {
        return undefined;
    }

    getUnique(): string | undefined {
        return undefined;
    }

    #repository: uSyncActionRepository;
    #uSyncIconRegistry: uSyncIconRegistry;
    #signalRContext: uSyncSignalRContext | null = null;

    /**
     * @type Boolean 
     * @description true when the workspace context has loaded (bound to auth)
     */
    #loaded = new UmbBooleanState(false);
    public readonly loaded = this.#loaded.asObservable();

    /**
     * @type Array<SyncActionGroup>
     * @description list of actions that have been returned
     */
    #actions = new UmbArrayState<SyncActionGroup>([], (x) => x.key);
    public readonly actions = this.#actions.asObservable();

    /**
     * @type Array<SyncHandlerSummary>
     * @description the summary objects that show the handler boxes
     */
    #workingActions = new UmbArrayState<SyncHandlerSummary>([], (x) => x.name);
    public readonly currentAction = this.#workingActions.asObservable();

    /**
     * @type Boolean
     * @description flag to say if things are currently being processed
     */
    #working = new UmbBooleanState(false);
    public readonly working = this.#working.asObservable();

    /** 
     * @type Boolean
     * @description flat to say that the last run has been completed (so results will show)
     */
    #completed = new UmbBooleanState(false);
    public readonly completed = this.#completed.asObservable();

    /**
     * @type Array<uSyncActionView>
     * @description the results of a run.
     */
    #results = new UmbArrayState<uSyncActionView>([], (x) => x.name);
    public readonly results = this.#results.asObservable();

    /**
     * @type uSyncSettings
     * @description current settings for uSync
     */
    #settings = new UmbObjectState<uSyncSettings | undefined>(undefined);
    public readonly settings = this.#settings?.asObservable();

    #handlerSettings = new UmbObjectState<uSyncHandlerSetSettings | undefined>(undefined);
    public readonly handlerSettings = this.#handlerSettings?.asObservable();

    constructor(host: UmbControllerHost) {
        super(host);

        console.log('workspace-context');

        this.provideContext(USYNC_CORE_CONTEXT_TOKEN, this);
        this.provideContext(UMB_WORKSPACE_CONTEXT, this);

        this.#repository = new uSyncActionRepository(this);
        this.#uSyncIconRegistry = new uSyncIconRegistry();
        this.#uSyncIconRegistry.attach(this);

        this.consumeContext(UMB_AUTH_CONTEXT, (_auth) => {
            OpenAPI.TOKEN = () => _auth.getLatestToken();
            OpenAPI.WITH_CREDENTIALS = true;
            this.#loaded.setValue(true);
        });

        this.consumeContext(USYNC_SIGNALR_CONTEXT_TOKEN, (_signalr) => {
            console.log('signalr', _signalr.getClientId());
            this.#signalRContext = _signalr;
        });


    }
    

    async getActions() {
        const { data } = await this.#repository.getActions();

        if (data) {
            this.#actions.setValue(data);
        }
    }

    async getSettings() {
        const {data} = await this.#repository.getSettings();

        if (data) {
            this.#settings.setValue(data);
        }
    }

    async getDefaultHandlerSetSettings() {
        const {data} = await this.#repository.getHandlerSettings("default");

        if (data) {
            this.#handlerSettings.setValue(data);
        }
    }

    async performAction(group: SyncActionGroup, key: string) {
        var clientId = this.#signalRContext?.getClientId() ?? '';

        this.#working.setValue(true);
        this.#completed.setValue(false);
        this.#results.setValue([]);

        var complete = false;
        var id = '';
        var step: number = 0;

        do {

            const { data } = await this.#repository.performAction(id, group.key, key, step, clientId);

            if (data) {

                step++;

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