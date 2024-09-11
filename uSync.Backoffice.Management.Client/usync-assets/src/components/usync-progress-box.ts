import {
	customElement,
	LitElement,
	css,
	html,
	property,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	USYNC_SIGNALR_CONTEXT_TOKEN,
	SyncUpdateMessage,
	HandlerStatus,
	SyncHandlerSummary,
} from '@jumoo/uSync';
import { UUIInterfaceColor } from '@umbraco-cms/backoffice/external/uui';

/**
 * Provides the progress box while things happen.
 */
@customElement('usync-progress-box')
export class uSyncProcessBox extends UmbElementMixin(LitElement) {
	constructor() {
		super();

		this.consumeContext(USYNC_SIGNALR_CONTEXT_TOKEN, (_signalR) => {
			this.observe(_signalR.update, (_update) => {
				this.updateMsg = _update;
			});

			this.observe(_signalR.add, (_add) => {
				this.addMsg = _add;
			});
		});
	}

	@state()
	updateMsg?: SyncUpdateMessage;

	@state()
	addMsg: object = {};

	@property({ type: String })
	title: string = '';

	@property({ type: Array })
	actions?: Array<SyncHandlerSummary>;

	@property({ type: Boolean })
	complete: boolean = false;

	render() {
		if (!this.actions) return nothing;

		var actionHtml = this.actions?.map((action) => {
			return html`
				<div
					style="position: relative;"
					class="action 
                    ${action.status == HandlerStatus.COMPLETE ? 'complete' : ''} 
                    ${action.status == HandlerStatus.PROCESSING ? 'working' : ''}">
					<uui-icon .name=${action.icon ?? 'icon-box'}></uui-icon>
					${this.renderBadge(action)}
					<h5>${action.name ?? 'unknown'}</h5>
				</div>
			`;
		});

		return html`
			<uui-box>
				<h2>${this.title}</h2>
				<div class="action-list">${actionHtml}</div>
				<div class="update-box">${this.updateMsg?.message}</div>
			</uui-box>
		`;
	}

	renderBadge(action: SyncHandlerSummary) {
		if (action.status == HandlerStatus.PENDING) return;
		if (action.status == HandlerStatus.PROCESSING) {
			return html`<uui-badge color="positive" look="default">
				<uui-icon name="icon-sync"></uui-icon
			></uui-badge>`;
		}

		const color: UUIInterfaceColor = action.inError ? 'warning' : 'positive';
		const label = action.inError
			? 'Some errors occured duing import'
			: 'Changes imported successfully';

		if (!this.complete || action.changes == 0)
			return html`<uui-badge .color=${color} look="default" title=${label}
				><uui-icon name="icon-check"></uui-icon
			></uui-badge>`;
		return html`<uui-badge .color=${color} title=${label}>${action.changes}</uui-badge>`;
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
			min-width: 90px;
			color: var(--uui-color-text-alt);
			opacity: 0.67;
		}

		.action uui-icon {
			font-size: var(--uui-type-h3-size);
		}

		.action uui-badge uui-icon {
			font-size: 16px;
		}

		.complete {
			color: var(--uui-color-default-emphasis);
		}

		.working {
			color: var(--uui-color-positive);
			opacity: 1;
		}

		.update-box {
			font-weight: bold;
			text-align: center;
		}
	`;
}

export default uSyncProcessBox;
