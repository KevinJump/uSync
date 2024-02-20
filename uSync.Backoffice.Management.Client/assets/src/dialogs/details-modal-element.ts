import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { uSyncDetailsModalData, uSyncDetailsModalValue } from "./details-modal-token";
import { customElement, html } from "@umbraco-cms/backoffice/external/lit";

@customElement('usync-details-modal')
export class uSyncDetailsModalElement extends
    UmbModalBaseElement<uSyncDetailsModalData, uSyncDetailsModalValue> {

    constructor() {
        super();
    }

    connectedCallback(): void {
        super.connectedCallback();
    }

    handleClose() {
		this.modalContext?.reject();
	}

    render() {
        return html`
            <umb-body-layout headline="Changes : ${this.data?.item.name ?? ''}">

                <uui-box headline="Detected Changes">
                    <div slot="header">Things that are diffrent</div>
                    <usync-change-view .item=${this.data?.item}></usync-change-view>
                </uui-box>
                <div slot="actions">
                        <uui-button 
                            id="cancel"
                            label="Close"
                            @click="${this.handleClose}">Close</uui-button>
                    </div>
            </umb-body-layout>
        `;
    }
}

export default uSyncDetailsModalElement;