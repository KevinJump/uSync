import { css, customElement, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement, UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { SyncLegacyCheckResponse } from "../api";

@customElement('usync-legacy-modal')
export class uSyncLegacyModalElement extends 
    UmbModalBaseElement<SyncLegacyCheckResponse, string> 
{

    #handleClose() {
        this.modalContext?.reject();
    }

    render() {

        return html`
            <umb-body-layout headline="Legacy folder detected">
                <div class="content">
                    ${this.renderLegacyFolder(this.data?.legacyFolder)}
                    ${this.renderLegacyTypes(this.data?.legacyTypes)}
                    ${this.renderCopy()}
                </div>
                <div slot="actions">
                    <uui-button id="cancel" label="close" @click=${this.#handleClose}>Close</uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    renderLegacyFolder(folder: string | null | undefined) {
        return folder === undefined || folder === null ? nothing 
        : html`
            <uui-box headline="Legacy folder found at ${folder}">
                <div>
                    <p>uSync has found a legacy uSync folder at ${folder} <br/>
                    Its likely that the content in it will need coverting in someway</p>
                </div>
            </uui-box>
        `;
    }

    renderLegacyTypes(legacyTypes: Array<string> | undefined) {

        if (legacyTypes == undefined || legacyTypes.length == 0) { return nothing; }

        const legacyTypeHtml = this.data?.legacyTypes.map((datatype) => {
            return html`<li>${datatype}</li>`;
        });

        return html`
            <uui-box headline="Legacy DataTypes">
                <div>
                    <p>uSync also found some legacy data types:</p>
                    <ul>
                        ${legacyTypeHtml}
                    </ul>
                    <p>
                        You can convert these legacy types using <a href="https://github.com/Jumoo/uSyncMigrations" target="_blank">uSync.Migrations</a><br/>
                        <em>(In the full uSync release, we want to do this conversion here.)</em>
                    </p>
                </div>
            </uui-box>
        `;

    }

    renderCopy() {
        return html`
            <uui-box headline="Move folder to v14">
                <p>Meanwhile, you can copy your ${this.data?.legacyFolder} folder <br/>
                to the uSync/v14 folder and run an import.</p>
                <p><strong>Remove or rename the ${this.data?.legacyFolder} folder to remove this popup</strong></p>
                <p>If nothing needs converting, then everything should import.<br/>
                    but this is a beta, on a beta.</p>
            </uui-box>
        `
    };

    static styles = css`
        uui-box {
            margin-bottom: 10px;
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