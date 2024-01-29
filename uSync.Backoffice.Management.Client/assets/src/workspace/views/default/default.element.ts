import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api"
import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";

import { USYNC_ACTION_CONTEXT_TOKEN, uSyncWorkspaceActionContext } from '../../context/action.context.js';

import "../../../shared/action-box.js";
import { SyncActionGroup } from "../../../api/index.js";

@customElement('usync-default-view')
export class uSyncDefaultViewElement extends UmbElementMixin(LitElement) {

    #actionContext? : uSyncWorkspaceActionContext;

    @property({ type: Array })
    actions?: Array<SyncActionGroup>

    @property({ type: Boolean })
    loaded: Boolean = false;

    constructor() {
        super();

        this.consumeContext(USYNC_ACTION_CONTEXT_TOKEN, (_instance) => {

            console.log('consume context');

            this.#actionContext = _instance;

            this.observe(_instance.actions, (_actions) => {
                console.log('actions', _actions);
                this.actions = _actions;

                this.loaded = this.actions !== null;
            });

            _instance.getActions();

            // _instance.getTime();
        });
    }


    /**
     * 
     * @param event 
     */
    performAction(event: CustomEventInit) {
        this.#actionContext?.performAction(event.detail.group, event.detail.key);
    }

    render() {

        if (this.loaded == false) {
            return html`<uui-loader></uui-loader>`;
        }
        else {

            console.log(this.actions?.length);

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
            `;
        }
    };

    static styles = [
        css`
            usync-action-box {
               margin: var(--uui-size-layout-1);
            }

            .action-buttons-box {
               margin: var(--uui-size-layout-1);
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