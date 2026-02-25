import {
	css,
	customElement,
	html,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

import {
	USYNC_CORE_CONTEXT_TOKEN,
	uSyncWorkspaceContext,
	SyncActionGroup,
	SyncHandlerSummary,
	SyncLegacyCheckResponse,
	USyncActionView,
	SyncSelectableSet,
	USYNC_SIGNALR_CONTEXT_TOKEN,
} from '@jumoo/uSync';
import { uSyncActionPerformEvent } from '../../components/events';
import { UmbRequestReloadChildrenOfEntityEvent } from '@umbraco-cms/backoffice/entity-action';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';
import { UMB_DOCUMENT_TYPE_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/document-type';
import { UMB_MEDIA_TYPE_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/media-type';
import { UMB_TEMPLATE_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/template';
import { UMB_DATA_TYPE_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/data-type';
import { UMB_MEMBER_TYPE_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/member-type';
import { UMB_STYLESHEET_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/stylesheet';
import { UMB_PARTIAL_VIEW_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/partial-view';
import { UMB_SCRIPT_ROOT_ENTITY_TYPE } from '@umbraco-cms/backoffice/script';

@customElement('usync-default-view')
export class uSyncDefaultViewElement extends UmbLitElement {
	#actionContext?: uSyncWorkspaceContext;

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
	_inBackground: boolean = false;

	@state()
	_connected: boolean = false;

	@state()
	_showProgress: boolean = false;

	@state()
	_group?: SyncActionGroup;

	@state()
	_results: Array<USyncActionView> = [];

	@state()
	_disabled: boolean = false;

	@state()
	_setName: string = 'Default';

	@state()
	_sets: Array<SyncSelectableSet> = [];

	constructor() {
		super();

		this.consumeContext(USYNC_SIGNALR_CONTEXT_TOKEN, (_signalR) => {
			if (!_signalR) return;

			this.observe(_signalR.connected, (_connected) => {
				this._connected = _connected;
			});
		});

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			if (!_instance) return;
			this.#actionContext = _instance;

			_instance.getSettings();

			this.observe(_instance.settings, (_settings) => {
				if (!_settings) return;

				this._setName = _settings.defaultSet ?? 'Default';

				this.#actionContext?.checkLegacy();
				this.#actionContext?.getHandlerSets();
			});

			this.observe(_instance.sets, (sets) => {
				if (!sets) return;
				this._sets = sets;
				this.#actionContext?.getActions(this._setName);
			});

			this.observe(_instance.actions, (_actions) => {
				if (!_actions || _actions.length == 0) return;
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

			this.observe(_instance.completed, async (_completed) => {
				this._completed = _completed;
				if (this._completed) {
					this._buttonState = 'success';

					await this.#refreshTrees();
				}
			});

			this.observe(_instance.legacy, (_legacy) => {
				this._legacy = _legacy;
			});

			this.observe(_instance.inBackground, (_inBackground) => {
				this._inBackground = _inBackground;
			});

			this.observe(_instance.workingGroup, (_workingGroup) => {
				if (_workingGroup) {
					this._group = _workingGroup;
				}
			});
		});
	}

	async #refreshTrees() {
		const actionEventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
		const refreshTypes = [
			UMB_DOCUMENT_TYPE_ROOT_ENTITY_TYPE,
			UMB_MEDIA_TYPE_ROOT_ENTITY_TYPE,
			UMB_MEMBER_TYPE_ROOT_ENTITY_TYPE,
			UMB_TEMPLATE_ROOT_ENTITY_TYPE,
			UMB_DATA_TYPE_ROOT_ENTITY_TYPE,
			UMB_STYLESHEET_ROOT_ENTITY_TYPE,
			UMB_PARTIAL_VIEW_ROOT_ENTITY_TYPE,
			UMB_SCRIPT_ROOT_ENTITY_TYPE,
		];

		refreshTypes.forEach((type) => {
			const event = new UmbRequestReloadChildrenOfEntityEvent({
				entityType: type,
				unique: null,
			});
			actionEventContext?.dispatchEvent(event);
		});
	}

	/**
	 * @method performAction
	 * @param {uSyncActionPerformEvent} event
	 * @description do a thing, (report, import, export)
	 */
	#performAction(event: uSyncActionPerformEvent) {
		if (!event) return;

		this._showProgress = true;

		this.#actionContext?.performAction({
			setName: this._setName,
			group: event.group,
			action: event.key,
			force: event.force ?? false,
			clean: event.clean ?? false,
			file: event.file ?? false,
		});
	}

	render() {
		if (this._loaded == false) {
			return html`<uui-loader></uui-loader>`;
		} else {
			return html`
				<umb-body-layout>
					${this.#renderLegacyBanner()} ${this.#renderSetPicker()}
					<div class="wrapper">
						${this.#renderActions()} ${this.#renderBanner()}
						${this.#renderBackgroundBanner()}
						${this.#renderProcessBox()}${this.#renderReport()}
					</div>
				</umb-body-layout>
			`;
		}
	}

	#renderSetPicker() {
		if (this._sets.length < 2) return nothing;

		var options = this._sets.map((set) => {
			return {
				name: set.name,
				value: set.name,
				selected: set.name === this._setName,
			};
		});

		return html`<div class="set-picker">
			<label for="set-select"
				>${this.localize.term('USyncSettings_currentHandlerSet')}</label
			>
			<uui-select
				id="set-select"
				.label=${this.localize.term('USyncSettings_currentHandlerSet')}
				.options=${options}
				@change=${(e: Event) => {
					const select = e.target as HTMLSelectElement;
					this._setName = select.value;
					this.#actionContext?.getActions(this._setName);
				}}>
			</uui-select>
		</div> `;
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
		if (!this._actions || !Array.isArray(this._actions)) return nothing;

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
		if (this._showProgress === true || this._completed === true) return nothing;

		return html`
			<umb-empty-state>
				<h2>
					<uui-icon name="usync-logo"></uui-icon>
					<umb-localize key="uSync_banner"></umb-localize>
				</h2>
			</umb-empty-state>
		`;
	}

	#renderProcessBox() {
		if (this._showProgress == false && this._completed == false) return nothing;

		return html`
			<usync-progress-box
				.title=${this._group?.groupName ?? 'doh!'}
				.actions=${this._workingActions}
				.complete=${this._completed}></usync-progress-box>
		`;
	}

	#renderReport() {
		if (!this._completed) return nothing;

		return html`<usync-results .results=${this._results}></usync-results>`;
	}

	#renderBackgroundBanner() {
		if (!this._inBackground || !this._working) return nothing;

		if (!this._connected) {
			return html` <uui-box class="banner warning">
				<uui-icon name="icon-alert"></uui-icon>
				${this.localize.term('uSync_runningInBackground')}
				<br />
				${this.localize.term('uSync_connectionLost')}
			</uui-box>`;
		}

		return html`<uui-box class="banner info">
			<uui-icon name="icon-info"></uui-icon>
			${this.localize.term('uSync_runningInBackground')}
		</uui-box>`;
	}

	static styles = [
		css`
			:host {
				display: block;
				margin-top: calc(var(--uui-size-space-4) * -1);
			}

			.wrapper {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-4);
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
			}

			.action-buttons-box {
				display: grid;
				grid-template-columns: repeat(auto-fit, minmax(430px, 1fr));
				position: relative;
				gap: var(--uui-size-space-4);
				flex-wrap: wrap;
				align-content: stretch;
				z-index: 1;
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
				top: var(--uui-size-2);
			}

			.set-picker {
				display: flex;
				flex-direction: row;
				align-items: center;
				justify-content: flex-end;
				gap: var(--uui-size-space-2);
				margin-bottom: var(--uui-size-space-4);
				border: 1px solid var(--uui-color-border);
				padding: var(--uui-size-space-4);
			}

			.set-picker label {
				font-weight: 700;
			}

			.set-picker label::after {
				content: ':';
			}

			.info {
				background-color: var(--uui-color-positive);
				color: var(--uui-color-positive-contrast);
			}

			.warning {
				background-color: var(--uui-color-warning);
				color: var(--uui-color-warning-contrast);
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
