import {
	LitElement,
	customElement,
	html,
	css,
	property,
	ifDefined,
} from '@umbraco-cms/backoffice/external/lit';
import { SyncActionGroup } from '@jumoo/uSync';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import {
	uSyncActionButtonClickEvent,
	uSyncActionPerformEvent,
} from '../workspace/components/events';

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

	@property({ type: Boolean })
	disabled: boolean = false;

	#onAction(e: uSyncActionButtonClickEvent, group: SyncActionGroup) {
		if (!e?.button) return;

		this.dispatchEvent(
			new uSyncActionPerformEvent(
				group,
				e.button.key,
				e.button.force,
				e.button.clean,
				e.button.file,
			),
		);
	}

	render() {
		const dropdownButtons = this.group.buttons.map((b) => {
			return html`
				<usync-action-button
					.button=${b}
					.disabled=${this.disabled}
					state=${ifDefined(this.state)}
					@usync-action-click=${(e: uSyncActionButtonClickEvent) =>
						this.#onAction(e, this.group)}></usync-action-button>
			`;
		});

		return html`
			<uui-box class="action-box ${this.disabled ? 'disabled' : ''}">
				<div class="box-content">
					<h2 class="box-heading">${this.group?.groupName}</h2>
					<umb-icon name=${this.group?.icon}></umb-icon>
					<div class="box-buttons">${dropdownButtons}</div>
				</div>
			</uui-box>
		`;
	}

	static styles = css`
		:host {
			flex-grow: 1;
		}

		.action-box {
			transition: opacity 0.2s ease-in-out;
		}

		.box-content {
			display: flex;
			flex-direction: column;
			align-items: center;
		}

		.box-heading {
			font-size: var(--uui-size-8);
			margin: 0;
		}

		umb-icon {
			margin: var(--uui-size-8) 0 var(--uui-size-10);
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

		.disabled {
			opacity: 0.4;
		}
	`;
}

export default uSyncActionBox;
