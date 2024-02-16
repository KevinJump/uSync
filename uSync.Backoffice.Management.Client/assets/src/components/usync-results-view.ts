import { LitElement, html, customElement, property, nothing, css } from "@umbraco-cms/backoffice/external/lit";
import { uSyncActionView } from "../api";


@customElement('usync-results')
export class uSyncResultsView extends LitElement {

    @property({type: Array})
    results : Array<uSyncActionView> = [];

    render() {

        var rowsHtml = this.results.map((result) => {
            return html`
                <uui-table-row>
                    <uui-table-cell><uui-icon .name=${result.success ? 'icon-check' : 'icon-wrong'}></uui-icon></uui-table-cell>
                    <uui-table-cell>${result.change}</uui-table-cell>
                    <uui-table-cell>${result.itemType}</uui-table-cell>
                    <uui-table-cell>${result.name}</uui-table-cell>
                    <uui-table-cell>${result.details.length > 0 ? 'Show details' : nothing}</uui-table-cell>
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