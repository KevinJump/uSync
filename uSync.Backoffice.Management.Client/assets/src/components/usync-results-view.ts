import { LitElement, html, customElement, property, nothing, css } from "@umbraco-cms/backoffice/external/lit";
import { uSyncActionView } from "../api";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_MODAL_MANAGER_CONTEXT, UmbModalManagerContext } from "@umbraco-cms/backoffice/modal";
import { USYNC_DETAILS_MODAL } from "../dialogs/details-modal-token";


@customElement('usync-results')
export class uSyncResultsView extends UmbElementMixin(LitElement) {

    private _modalContext? : UmbModalManagerContext;

    constructor() {
        super();

        this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (_instance) => {
            this._modalContext = _instance;
        });
    }

    @property({type: Array})
    results : Array<uSyncActionView> = [];

    async _openDetailsView(result: uSyncActionView) {

        const detailsModal = this._modalContext?.open(USYNC_DETAILS_MODAL, {
            data : {
                item: result
            }
        });

        const data = await detailsModal?.onSubmit();
        if (!data) return;
    }

    render() {

        var rowsHtml = this.results.map((result) => {
            return html`
                <uui-table-row>
                    <uui-table-cell><uui-icon .name=${result.success ? 'icon-check' : 'icon-wrong'}></uui-icon></uui-table-cell>
                    <uui-table-cell>${result.change}</uui-table-cell>
                    <uui-table-cell>${result.itemType}</uui-table-cell>
                    <uui-table-cell>${result.name}</uui-table-cell>
                    <uui-table-cell>${result.details.length > 0 ? this.renderDetailsButton(result) : nothing}</uui-table-cell>
                </uui-table-row>
            `;
        });

        return html`

            <div class="result-header">
                ${this.results.length} items
            </div>

            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Success</uui-table-head-cell>
                    <uui-table-head-cell>Change</uui-table-head-cell>
                    <uui-table-head-cell>Type</uui-table-head-cell>
                    <uui-table-head-cell>Name</uui-table-head-cell>
                    <uui-table-head-cell>Details</uui-table-head-cell>
                </uui-table-head>

                ${rowsHtml}

            </uui-table>
        `;
    }

    renderDetailsButton(result: uSyncActionView) {
        return html`
            <uui-button look='default' color='positive' label='show details'
                @click=${() => this._openDetailsView(result)}></uui-button>
        `;
    }

    static styles = css`

        uui-table {
            position: relative;
            z-index: 100;
        }

        .result-header {
            display: flex;
            justify-content: flex-end;
        }
    `
}

export default uSyncResultsView;