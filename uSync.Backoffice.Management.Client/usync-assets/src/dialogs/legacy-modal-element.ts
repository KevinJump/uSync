import { css, customElement, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement, UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { SyncLegacyCheckResponse } from "../api";

@customElement('usync-legacy-modal')
export class uSyncLegacyModalElement extends 
    UmbModalBaseElement<SyncLegacyCheckResponse, string> 
{

    #onClose() {
        this.modalContext?.reject();
    }

    render() {

        return html`
            <umb-body-layout headline="Legacy uSync folder detected">
                <div class="content">
                    ${this.renderLegacyFolder(this.data?.legacyFolder)}
                    ${this.renderLegacyTypes(this.data?.legacyTypes)}
                    ${this.renderCopy()}
                </div>
                <div slot="actions">
                    <uui-button id="cancel" label="close" @click=${this.#onClose}>Close</uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    renderLegacyFolder(folder: string | null | undefined) {
        return folder === undefined || folder === null ? nothing 
        : html`
            <p>uSync has found a legacy uSync folder at <strong>${folder}</strong>. <br/>
            Its likely that the content in it will need coverting in someway</p>
        `;
    }

    renderLegacyTypes(legacyTypes: Array<string> | undefined) {

        if (legacyTypes == undefined || legacyTypes.length == 0) { return nothing; }

        const legacyTypeHtml = this.data?.legacyTypes.map((datatype) => {
            return html`<li>${datatype}</li>`;
        });

        return html`
                <div>
                    <h4>Obsolete DataTypes</h4>
                    <ul>
                        ${legacyTypeHtml}
                    </ul>
                    <p>
                        You can convert these DataTypes using <a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br/>
                        <em>(In the full uSync release conversion will happen here.)</em>
                    </p>
                </div>
        `;

    }

    renderCopy() {
        return html`
            <h4>Copy to uSync/v14</h4>
            <p>You can copy your ${this.data?.legacyFolder} folder to the ~/uSync/v14 folder<br/> and run an import.</p>
            <p>If nothing needs converting, then everything should import.<br/>
                but this is a beta release running on a beta release 🤞</p>
            <p><strong>Remove or rename the ${this.data?.legacyFolder} folder to prevent this popup</strong></p>
        `
    };

    static styles = css`

        .content {
            margin: -10px 0;
        }

        em {
            color: var(--uui-color-positive);
        }
    `;
}

export default uSyncLegacyModalElement;

export const USYNC_LEGACY_MODAL = new UmbModalToken<SyncLegacyCheckResponse, string>(
    'usync.legacy.modal',
    {
        modal: {
            type: 'dialog',
            size: 'small'
        }
    });