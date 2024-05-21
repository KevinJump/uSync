import {
	css,
	customElement,
	html,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';

import {
	USYNC_CORE_CONTEXT_TOKEN,
	uSyncWorkspaceContext,
} from '../../workspace.context.js';
import {
	SyncActionGroup,
	SyncHandlerSummary,
	SyncLegacyCheckResponse,
	uSyncActionView,
} from '../../../api/index.js';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

@customElement('usync-default-view')
export class uSyncDefaultViewElement extends UmbLitElement {
	#actionContext?: uSyncWorkspaceContext;
	#contextLoaded: Boolean = false;

	@state()
	_actions?: Array<SyncActionGroup>;

	@state()
	_workingActions?: Array<SyncHandlerSummary>;

	@state()
	_loaded: Boolean = false;

	@state()
	_legacy?: SyncLegacyCheckResponse;

	@state()
	_buttonState: UUIButtonState;

	@state()
	_working: boolean = false;

	@state()
	_completed: boolean = false;

	@state()
	_showProgress: boolean = false;

	@state()
	_group?: SyncActionGroup;

	@state()
	_results: Array<uSyncActionView> = [];

	@state()
	_disabled: boolean = false;

	constructor() {
		super();

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			this.#actionContext = _instance;

			this.observe(_instance.actions, (_actions) => {
				this._actions = _actions;
				this._loaded = this._actions !== null;
			});

			this.observe(_instance.currentAction, (_currentAction) => {
				this._workingActions = _currentAction;
			});

			this.observe(_instance.working, (_working) => {
				this._working = _working;

				if (this._working) {
					this._buttonState = 'waiting';
					this._disabled = true;
				} else {
					this._disabled = false;
				}
			});

			this.observe(_instance.results, (_results) => {
				this._results = _results;
			});

			this.observe(_instance.completed, (_completed) => {
				this._completed = _completed;
				if (this._completed) {
					this._buttonState = 'success';
				}
			});

			this.observe(_instance.legacy, (_legacy) => {
				this._legacy = _legacy;
			});

			if (this.#contextLoaded == false) {
				this.#actionContext?.checkLegacy();
				this.#actionContext?.getActions();
				this.#contextLoaded;
			}
		});
	}

	/**
	 * @method performAction
	 * @param {CustomEventInit} event
	 * @description do a thing, (report, import, export)
	 */
	#performAction(event: CustomEventInit) {
		this._showProgress = true;
		this._group = event.detail.group;
		this.#actionContext?.performAction({
			group: event.detail.group,
			action: event.detail.key,
			force: event.detail.force,
			clean: event.detail.clean,
		});
	}

	render() {
		if (this._loaded == false) {
			return html`<uui-loader></uui-loader>`;
		} else {
			return html`
				<umb-body-layout>
					${this.#renderLegacyBanner()} ${this.#renderActions()} ${this.#renderBanner()}
					${this.#renderProcessBox()} ${this.#renderReport()}
				</umb-body-layout>
			`;
		}
	}

	#renderLegacyBanner() {
		return !this._legacy?.hasLegacy
			? nothing
			: html`
					<div class="legacy-banner">
						<umb-icon name="icon-alert"></umb-icon>
						${this.localize.term('uSync_legacyBanner')}
					</div>
				`;
	}

	#renderActions() {
		var actions = this._actions?.map((group) => {
			return html`
				<usync-action-box
					.disabled=${this._disabled}
					.group="${group}"
					.state=${this._buttonState}
					@perform-action=${this.#performAction}>
				</usync-action-box>
			`;
		});

		return html` <div class="action-buttons-box">${actions}</div> `;
	}

	#renderBanner() {
		if (this._showProgress === true) return nothing;

		return html`
			<umb-empty-state>
				<h2>
					<usync-icon-registry>
						<uui-icon name="usync-logo"></uui-icon>
					</usync-icon-registry>
					<umb-localize key="uSync_banner"></umb-localize>
				</h2>
			</umb-empty-state>
		`;
	}

	#renderProcessBox() {
		if (this._showProgress == false) return nothing;

		return html`
			<usync-progress-box
				.title=${this._group?.groupName ?? 'doh!'}
				.actions=${this._workingActions}></usync-progress-box>
		`;
	}

	#renderReport() {
		if (!this._completed) return nothing;

		return html`
			<uui-box class="results-box">
				<usync-results .results=${this._results}></usync-results>
			</uui-box>
		`;
	}

	static styles = [
		css`
			:host {
				display: block;
				margin-top: calc(var(--uui-size-space-4) * -1);
			}

			.legacy-banner {
				display: flex;
				gap: var(--uui-size-space-2);
				padding: var(--uui-size-space-4);
				margin: var(--uui-size-space-4) 0;
				background-color: var(--uui-color-warning);
				color: var(--uui-color-warning-contrast);
			}

			.results-box {
				position: relative;
				display: block;
				z-index: 1;
				margin: var(--uui-size-space-4) 0;
			}

			.action-buttons-box {
				position: relative;
				display: flex;
				flex-wrap: wrap;
				align-content: stretch;
				z-index: 1;
				margin: calc(var(--uui-size-space-2) * -1);
			}

			umb-empty-state {
				position: absolute;
				top: 50%;
				transform: translateY(-50%);
				left: 0;
				right: 0;
				margin: 0 auto;
				text-align: center;
				color: var(--uui-color-border);
				z-index: 0;
			}

			umb-empty-state h2 {
				font-size: var(--uui-type-h2-size);
			}

			umb-empty-state uui-icon {
				position: relative;
				top: var(--uui-size-4);
			}
		`,
	];
}

export default uSyncDefaultViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-default-view': uSyncDefaultViewElement;
	}
}
