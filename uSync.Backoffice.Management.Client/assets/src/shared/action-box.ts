import { LitElement, customElement, html, css, property } from "@umbraco-cms/backoffice/external/lit";
import { SyncActionButton, SyncActionGroup } from "../api";

/**
 * @exports
 * @class uSyncActionBox
 */

@customElement('usync-action-box')
export class uSyncActionBox extends LitElement {


    @property({ type: String })
    myName = ''; 

    /**
     * @type: {uSyncActionGroup}
     * @memberof uSyncActionBox
     * @description collection of buttons to display.
     */
    @property({ type: Object })
    group! : SyncActionGroup ;
   

    private _handleClick(groupKey: string | undefined, button: SyncActionButton) {
        this.dispatchEvent(new CustomEvent('perform-action', {
            detail: {
                group: groupKey,
                key: button.key
            }
        }));
    }

    render() {

        const groupKey = this.group?.key;

        const buttons = this.group?.buttons.map((i) => {
            return html`
                <uui-button label=${i.key} 
                    color=${i.color}
                    look=${i.look}
                    style="font-size: 20px"
                    @click=${() => this._handleClick(groupKey, i)}
                    ></uui-button>
            `;
        });

        return html`
                <uui-box class='action-box'>

                    <div class="box-content">

                        <h2 class="box-heading">${this.group?.groupName}</h2>

                        <uui-icon name=${this.group?.icon}></uui-icon>
                    
                        <div class="box-buttons">
                            ${buttons}
                        </div>
                        
                    </div>
                </uui-box>
        `;
    }


    static styles = css`

        .box-content {
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .box-heading {
            font-size: 20pt;
        }

        uui-icon {
            margin: 20px;
            font-size: 40pt;
        }

        uui-button {
            margin: 0 5px;
        }

        .box-buttons {
            margin-top: 10px;
        }
        `;
}

export default uSyncActionBox;


