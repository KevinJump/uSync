import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api"
import { LitElement, css, customElement, html, nothing, property } from "@umbraco-cms/backoffice/external/lit";

import { USYNC_CORE_CONTEXT_TOKEN, uSyncWorkspaceContext } from '../../workspace.context.js';
import { ActionInfo, SyncActionGroup } from "../../../api/index.js";

@customElement('usync-default-view')
export class uSyncDefaultViewElement extends UmbElementMixin(LitElement) {

    #actionContext? : uSyncWorkspaceContext;
    #contextLoaded: Boolean = false; 

    @property({ type: Array })
    actions?: Array<SyncActionGroup>

    @property({ type: Boolean })
    loaded: Boolean = false;

    @property({ type: Array  })
    workingActions? : Array<ActionInfo>;

    @property({ type: Boolean})
    working: boolean = false; 

    @property({ type: Boolean})
    completed: boolean = false; 

    @property({ type: Boolean}) 
    showProgress: boolean = false;

    @property({ type: String}) 
    group: string = "";

    constructor() {
        super();
        console.log('element constructor');
    }

    #consumeContext() {

        this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
            console.log('consume context');
            this.#actionContext = _instance;

            this.observe(_instance.actions, (_actions) => {
                this.actions = _actions;
                this.loaded = this.actions !== null;
            });

            this.observe(_instance.currentAction, (_currentAction) => {
                this.workingActions = _currentAction;
            });

            this.observe(_instance.working, (_working) => {
                this.working = _working;
            });

            this.observe(_instance.completed, (_completed) => {
                this.completed = _completed;
            });

            this.observe(_instance.loaded, (_loaded) => {
                if (_loaded && this.#contextLoaded == false) {
                    this.#actionContext?.getActions();
                    this.#contextLoaded
                }
            })

        });

    }


    connectedCallback(): void {
        super.connectedCallback();
        console.log('connected');
        this.#consumeContext();
    }

    /**
     * 
     * @param event 
     */
    performAction(event: CustomEventInit) {
        this.showProgress = true;

        console.log(event.detail);
        this.group = event.detail.group;
        this.#actionContext?.performAction(event.detail.group, event.detail.key);
    }

    render() {

        if (this.loaded == false) {
            return html`<uui-loader></uui-loader>`;
        }
        else {

            console.log('element actions', this.actions?.length);

            var actions = this.actions?.map((group) => {
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
        if (this.showProgress == false) return nothing;

        console.log('element working actions', this.workingActions?.length);

        return html`
            <usync-progress-box .title=${this.group}
                .actions=${this.workingActions}></usync-progress-box>
        `;
    }

    #renderReport() {
        if (this.completed == false) return nothing;

        return html`
            <uui-box>
                <h2>Report Here</h2>
            </uui-box>
        `
    }

    static styles = [
        css`
            usync-action-box, uui-box {
               margin: var(--uui-size-layout-1);
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