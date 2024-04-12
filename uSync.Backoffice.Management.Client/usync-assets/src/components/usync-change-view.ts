import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api'
import {
    LitElement,
    css,
    customElement,
    html,
    property,
} from '@umbraco-cms/backoffice/external/lit'
import { ChangeType, uSyncActionView } from '../api'
import * as Diff from 'diff'

/**
 * shows the change details for an item.
 */
@customElement('usync-change-view')
export class uSyncChangeView extends UmbElementMixin(LitElement) {
    @property({ type: Object })
    item?: uSyncActionView

    render() {
        if (this.item?.change == ChangeType.CREATE) {
            return this.render_create()
        }

        return html`
            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Action</uui-table-head-cell>
                    <uui-table-head-cell>Item</uui-table-head-cell>
                    <uui-table-head-cell>Diffrence</uui-table-head-cell>
                </uui-table-head>
                ${this.render_details()}
            </uui-table>
        `
    }

    render_create() {
        return html` <h1>This item is being created</h1> `
    }

    #getJsonOrString(value: string | null | undefined) {
        try {
            return JSON.stringify(JSON.parse(value ?? ''), null, 1)
        } catch {
            return value ?? ''
        }
    }

    render_details() {
        var changesHtml = this.item?.details.map((detail) => {
            const oldValue = this.#getJsonOrString(detail.oldValue)
            const newValue = this.#getJsonOrString(detail.newValue)
            const changes = Diff.diffWords(oldValue, newValue)

            const changeHtml = changes.map((change: any) => {
                console.log(change)

                if (change.added) {
                    return html`<ins>${change.value}</ins>`
                } else if (change.removed) {
                    return html`<del>${change.value}</del>`
                } else {
                    return html`<span>${change.value}</span>`
                }
            })

            return html`
                <uui-table-row>
                    <uui-table-cell>${detail.name}</uui-table-cell>
                    <uui-table-cell>${detail.change}</uui-table-cell>
                    <uui-table-cell class="detail-data">
                        <pre>${changeHtml}</pre>
                    </uui-table-cell>
                </uui-table-row>
            `
        })

        return changesHtml
    }

    render_changes() {}

    static styles = css`
        :host {
            display: block;
            margin: var(--uui-size-space-4) 0;
        }

        uui-table-cell {
            vertical-align: top;
        }

        uui-table-cell pre {
            margin: 0;
            padding: 0;
        }

        pre ins {
            color: var(--uui-color-positive);
        }

        pre del {
            color: var(--uui-color-danger);
        }
    `
}

export default uSyncChangeView
