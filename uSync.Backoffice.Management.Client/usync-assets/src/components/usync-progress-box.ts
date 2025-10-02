import {
	customElement,
	LitElement,
	css,
	html,
	property,
	nothing,
	state,
	classMap,
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
			if (!_signalR) return;

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

	@state()
	showProgress: boolean = true;

	#hideProgress() {
		window.setTimeout(() => {
			this.showProgress = false;
		}, 2000);
	}

	render() {
		if (!this.actions) return nothing;

		let progress = 0;
		const actionCount = this.actions.length;
		const boxSize = 100 / actionCount;
		const actionProgress = (this.updateMsg?.count ?? 0) / (this.updateMsg?.total ?? 1);

		let actionHtml = this.actions?.map((action) => {
			if (action.status == HandlerStatus.COMPLETE) progress++;

			return html`
				<div
					class="action 
                    ${action.status == HandlerStatus.COMPLETE ? 'complete' : ''} 
                    ${action.status == HandlerStatus.PROCESSING ? 'working' : ''}">
					<div class="icon-holder">
						<uui-icon .name=${action.icon ?? 'icon-box'}></uui-icon>
						${this.renderBadge(action)}
					</div>
					<h4>${action.name ?? 'unknown'}</h4>
				</div>
			`;
		});

		let overall = this.showProgress
			? progress * boxSize + actionProgress * boxSize - boxSize
			: 0;

		if (this.complete) {
			this.#hideProgress();
		} else {
			this.showProgress = true;
		}

		const progressClass = { hidden: !this.showProgress };

		return html`
			<uui-box>
				<h2>${this.title}</h2>
				<div class="action-list">${actionHtml}</div>
				<div class="update-box">${this.updateMsg?.message}</div>
				<uui-progress-bar
					progress=${overall}
					class=${classMap(progressClass)}></uui-progress-bar>
			</uui-box>
		`;
	}

	renderBadge(action: SyncHandlerSummary) {
		if (action.status == HandlerStatus.PENDING) return;
		if (action.status == HandlerStatus.PROCESSING) {
			return html`<uui-badge color="positive" look="default">
				<uui-icon name="icon-circle-dotted" class="rotating"></uui-icon
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
			min-width: var(--uui-size-layout-5);
			color: var(--uui-color-text-alt);
			opacity: 0.67;
			margin: var(--uui-size-space-4) 0 var(--uui-size-space-6);
		}

		.action h4 {
			margin: var(--uui-size-space-4) 0;
		}

		.icon-holder {
			position: relative;
			padding: 0 var(--uui-size-7);
		}

		.rotating {
			animation: spin-animation 2s infinite;
			animation-timing-function: linear;
			display: inline-block;
		}

		@keyframes spin-animation {
			0% {
				transform: rotate(360deg);
			}
			100% {
				transform: rotate(0deg);
			}
		}

		.action uui-icon {
			font-size: var(--uui-size-12);
		}

		.action uui-badge uui-icon {
			font-size: var(--uui-type-h4-size);
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

		uui-progress-bar {
			padding: 0;
			margin: 0;
		}

		.hidden {
			opacity: 0;
		}
	`;
}

export default uSyncProcessBox;
