import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api"
import { LitElement, css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";

import { USYNC_CORE_CONTEXT_TOKEN, uSyncWorkspaceContext } from '../../workspace.context.js';
import { SyncActionGroup, SyncHandlerSummary, uSyncActionView } from "../../../api/index.js";

@customElement('usync-default-view')
export class uSyncDefaultViewElement extends UmbElementMixin(LitElement) {

    #actionContext? : uSyncWorkspaceContext;
    #contextLoaded: Boolean = false; 

    @state()
    _actions?: Array<SyncActionGroup>

    @state()
    _workingActions? : Array<SyncHandlerSummary>;

    @state()
    _loaded: Boolean = false;

    @state()
    _working: boolean = false; 

    @state()
    _completed: boolean = false; 

    @state() 
    _showProgress: boolean = false;

    @state()
    _group: string = "";

    @state()
    _results: Array<uSyncActionView> = [];

    constructor() {
        super();
    }
  
    connectedCallback(): void {
        super.connectedCallback();
        this.#consumeContext();
    }

    #consumeContext() {

        this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
            this.#actionContext = _instance;

            this.observe(_instance.actions, (_actions) => {
                this._actions = _actions;
                this._loaded = this._actions !== null;
            });

            this.observe(_instance.currentAction, (_currentAction) => {
                this._workingActions = _currentAction;
            });

            this.observe(_instance.working, (_working) => {
                this._working = _working;
            });

            this.observe(_instance.results, (_results) => {
                this._results = _results;
            })

            this.observe(_instance.completed, (_completed) => {
                this._completed = _completed;
            });

            this.observe(_instance.loaded, (_loaded) => {
                if (_loaded && this.#contextLoaded == false) {
                    this.#actionContext?.getActions();
                    this.#contextLoaded
                }
            })

        });
    }

    /**
     * @method performAction
     * @param {CustomEventInit} event 
     * @description do a thing, (report, import, export)
     */
    performAction(event: CustomEventInit) {
        this._showProgress = true;

        console.log(event.detail);
        this._group = event.detail.group;
        this.#actionContext?.performAction(event.detail.group, event.detail.key);
    }

    render() {

        if (this._loaded == false) {
            return html`<uui-loader></uui-loader>`;
        }
        else {

            console.log('element actions', this._actions?.length);

            var actions = this._actions?.map((group) => {
                return html`
                <usync-action-box myName="fred"
                    .group="${group}"
                    @perform-action=${this.performAction}>
                </usync-action-box>
            `;
            })

            return html`
                <div class="action-buttons-box">
                    ${actions}
                </div>

                ${this.#renderProcessBox()}

                ${this.#renderReport()}
            `;
        }
    };

    #renderProcessBox() {
        if (this._showProgress == false) return nothing;

        console.log('element working actions', this._workingActions?.length);

        return html`
            <usync-progress-box .title=${this._group}
                .actions=${this._workingActions}></usync-progress-box>
        `;
    }

    #renderReport() {
        if (this._completed == false) return nothing;

        return html`
            <uui-box>
                <usync-results .results=${this._results}></usync-results>
            </uui-box>
        `
    }

    static styles = [
        css`
            usync-action-box, uui-box {
               margin: var(--uui-size-space-4);
            }

            .action-buttons-box {
               display: grid;
               grid-template-columns: 1fr 1fr 1fr;
            }        
        `
    ]
}

export default uSyncDefaultViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'usync-default-view': uSyncDefaultViewElement
    }
}