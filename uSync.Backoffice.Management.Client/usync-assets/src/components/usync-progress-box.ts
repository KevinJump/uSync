import {
    customElement,
    LitElement,
    css,
    html,
    property,
    nothing,
    state,
} from '@umbraco-cms/backoffice/external/lit'
import { HandlerStatus, SyncHandlerSummary } from '../api'
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api'
import { USYNC_SIGNALR_CONTEXT_TOKEN } from '../signalr/signalr.context.token'
import type { SyncUpdateMessage } from '../signalr/types'

/**
 * @class uSyncProcessBox
 * @description provides the progress box while things happen.
 */
@customElement('usync-progress-box')
export class uSyncProcessBox extends UmbElementMixin(LitElement) {
    constructor() {
        super()

        this.consumeContext(USYNC_SIGNALR_CONTEXT_TOKEN, (_signalR) => {
            this.observe(_signalR.update, (_update) => {
                this.updateMsg = _update
            })

            this.observe(_signalR.add, (_add) => {
                this.addMsg = _add
            })
        })
    }

    @state()
    updateMsg?: SyncUpdateMessage

    @state()
    addMsg: object = {}

    @property({ type: String })
    title: string = ''

    @property({ type: Array })
    actions?: Array<SyncHandlerSummary>

    render() {
        if (!this.actions) return nothing

        var actionHtml = this.actions?.map((action) => {
            return html`
                <div
                    class="action 
                    ${action.status == HandlerStatus.COMPLETE
                        ? 'complete'
                        : ''} 
                    ${action.status == HandlerStatus.PROCESSING
                        ? 'working'
                        : ''}"
                >
                    <uui-icon .name=${action.icon ?? 'icon-box'}></uui-icon>
                    <h4>${action.name ?? 'unknown'}</h4>
                </div>
            `
        })

        return html`
            <uui-box>
                <h2>${this.title}</h2>
                <div class="action-list">${actionHtml}</div>
                <div class="update-box">${this.updateMsg?.message}</div>
            </uui-box>
        `
    }

    static styles = css`
        :host {
            display: block;
            margin: var(--uui-size-space-4) 0;
        }

        h2 {
            text-align: center;
            margin: 0;
        }

        .action-list {
            margin-top: var(--uui-size-space-4);
            padding: var(--uui-size-space-4) 0;
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
        }

        .action {
            display: flex;
            flex-direction: column;
            align-items: center;
            min-width: var(--uui-size-32);
            color: var(--uui-color-text-alt);
        }

        .action uui-icon {
            font-size: var(--uui-size-9);
        }

        .complete {
            color: var(--uui-color-default-emphasis);
        }

        .working {
            color: var(--uui-color-positive);
        }

        .update-box {
            font-weight: bold;
            text-align: center;
        }
    `
}

export default uSyncProcessBox
