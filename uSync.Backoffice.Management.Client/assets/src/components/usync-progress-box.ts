import { customElement, LitElement, css, html, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { HandlerStatus, SyncHandlerSummary } from "../api";

/**
 * @class uSyncProcessBox
 * @description provides the progress box while things happen.
 */
@customElement('usync-progress-box')
export class uSyncProcessBox extends LitElement {

    @property({type: String})
    title: string = "";

    @property({type: Array})
    actions? : Array<SyncHandlerSummary>;

    render() {

        console.log('progress box', this.actions?.length);

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
    `;

}

export default uSyncProcessBox;