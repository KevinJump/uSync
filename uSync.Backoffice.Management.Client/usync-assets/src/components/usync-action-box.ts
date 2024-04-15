import {
	LitElement,
	customElement,
	html,
	css,
	property,
	ifDefined,
} from '@umbraco-cms/backoffice/external/lit';
import { SyncActionGroup } from '../api';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';

/**
 * displays the action buttons for a given group
 */
@customElement('usync-action-box')
export class uSyncActionBox extends LitElement {
	/**
	 * Collection of buttons to display.
	 */
	@property({ type: Object })
	group!: SyncActionGroup;

	/**
	 * state to display on buttons
	 */
	@property({ type: String })
	state?: UUIButtonState;

	#onAction(e: CustomEvent, group: SyncActionGroup) {
		if (!e.detail?.button) return;

		this.dispatchEvent(
			new CustomEvent('perform-action', {
				detail: {
					group: group,
					key: e.detail.button.key,
					force: e.detail.button.force,
					clean: e.detail.button.clean,
				},
			}),
		);
	}

	render() {
		const dropdownButtons = this.group.buttons.map((b) => {
			return html`
				<usync-action-button
					.button=${b}
					state=${ifDefined(this.state)}
					@usync-action-click=${(e: CustomEvent) =>
						this.#onAction(e, this.group)}></usync-action-button>
			`;
		});

		return html`
			<uui-box class="action-box">
				<div class="box-content">
					<h2 class="box-heading">${this.group?.groupName}</h2>
					<uui-icon name=${this.group?.icon}></uui-icon>
					<div class="box-buttons">${dropdownButtons}</div>
				</div>
			</uui-box>
		`;
	}

	static styles = css`
		:host {
			flex-grow: 1;
			margin: var(--uui-size-space-2);
		}

		.box-content {
			display: flex;
			flex-direction: column;
			align-items: center;
		}

		.box-heading {
			font-size: var(--uui-size-7);
			margin: 0;
		}

		uui-icon {
			margin: var(--uui-size-space-6);
			font-size: var(--uui-type-h2-size);
			color: var(--uui-color-text-alt);
		}

		uui-button {
			margin: 0 var(--uui-size-space-2);
			font-size: var(--uui-size-6);
		}

		.box-buttons {
			margin: var(--uui-size-space-2) 0;
		}
	`;
}

export default uSyncActionBox;
