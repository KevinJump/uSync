import { customElement, LitElement, css, html, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { HandlerStatus, SyncHandlerSummary } from "../api";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { ISyncUpdateMessage, USYNC_SIGNALR_CONTEXT_TOKEN } from "../signalr/signalr.context";

/**
 * @class uSyncProcessBox
 * @description provides the progress box while things happen.
 */
@customElement('usync-progress-box')
export class uSyncProcessBox extends UmbElementMixin(LitElement) {


    constructor() {
        super();

        this.consumeContext(USYNC_SIGNALR_CONTEXT_TOKEN, (_signalR) => {

            this.observe(_signalR.update, (_update) => {
                this.updateMsg = _update;
            });

            this.observe(_signalR.add, (_add) => {
                this.addMsg = _add;
            });

        });
    }

    @property({type: Object})
    updateMsg : ISyncUpdateMessage | null = null;

    @property({type: Object})
    addMsg : object = {};


    @property({type: String})
    title: string = "";

    @property({type: Array})
    actions? : Array<SyncHandlerSummary>;

    render() {

        if (!this.actions) return nothing; 

        var actionHtml = this.actions?.map((action) => {

            return html`
                <div class="action 
                    ${action.status == HandlerStatus.COMPLETE ? 'complete' : ''} 
                    ${action.status == HandlerStatus.PROCESSING ? "working" : ''}">
                    <uui-icon .name=${action.icon ?? "icon-box"}></uui-icon>
                    <h4>${action.name ?? "unknown"}</h4>
                </div>
            `;

        });


        return html`
            <uui-box>
                <h2>${this.title}</h2>
                <div class="action-list">
                    ${actionHtml}
                </div>
                <div class="update-box">
                    ${this.updateMsg?.message}
                </div>
            </uui-box>
        `;
    }
    
    static styles = css`
        uui-box {
            margin: var(--uui-size-space-4);
        }

        h2 {
            text-align: center;
        }

        .action-list {
            display: flex;
            justify-content: space-around;
        }

        .action {
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .action uui-icon {
            font-size: 30pt;
        }
        
        .complete {
            color: blue;
            opacity: 0.5;
        }

        .working {
            color: green;
        }

        .update-box {
            font-weight: bold;
            text-align: center;

        }
    `;

}

export default uSyncProcessBox;