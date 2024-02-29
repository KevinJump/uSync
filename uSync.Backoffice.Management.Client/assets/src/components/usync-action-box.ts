import { LitElement, customElement, html, css, property, ifDefined } from "@umbraco-cms/backoffice/external/lit";
import { SyncActionButton, SyncActionGroup } from "../api";
import { UUIButtonState, UUIInterfaceColor, UUIInterfaceLook } from "@umbraco-cms/backoffice/external/uui";

/**
 * @exports
 * @class uSyncActionBox
 * @fires perform-action - when the user clicks the buttons. 
 */
@customElement('usync-action-box')
export class uSyncActionBox extends LitElement {

    /**
     * @type: {uSyncActionGroup}
     * @memberof uSyncActionBox
     * @description collection of buttons to display.
     */
    @property({ type: Object })
    group! : SyncActionGroup ;

    @property({type: String})
    state: UUIButtonState | undefined = undefined;

    private _handleClick(group: SyncActionGroup, button: SyncActionButton) {
        this.dispatchEvent(new CustomEvent('perform-action', {
            detail: {
                group: group,
                key: button.key
            }
        }));
    }

    render() {

        const buttons = this.group?.buttons.map((i) => {
            return html`
                <uui-button label=${i.key} 
                    color=${<UUIInterfaceColor>i.color}
                    look=${<UUIInterfaceLook>i.look}
                    state=${ifDefined(this.state)}
                    @click=${() => this._handleClick(this.group, i)}
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
            font-size: 16pt;
        }

        uui-icon {
            margin: 10px;
            font-size: 30pt;
        }

        uui-button {
            margin: 0 5px;
            font-size: 14pt;
        }

        .box-buttons {
            margin-top: 10px;
        }
        `;
}

export default uSyncActionBox;


