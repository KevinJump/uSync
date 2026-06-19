import {
	css,
	customElement,
	html,
	ifDefined,
	nothing,
	property,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { SyncActionButton } from '@jumoo/uSync';
import {
	UUIButtonState,
	UUIInterfaceColor,
	UUIInterfaceLook,
} from '@umbraco-cms/backoffice/external/uui';
import { uSyncActionButtonClickEvent } from './events';

@customElement('usync-action-button')
export class SyncActionButtonElement extends UmbLitElement {
	@property({ type: Object })
	button?: SyncActionButton;

	@property({ type: String })
	state?: UUIButtonState;

	@property({ type: Boolean })
	disabled: boolean = false;

	@state()
	_popoverOpen: boolean = false;

	#onClick(item?: SyncActionButton) {
		if (!item) return;

		this.dispatchEvent(new uSyncActionButtonClickEvent(item));
	}

	render() {
		return (this.button?.children?.length ?? 0) > 0
			? this.renderMultipleActions(this.button)
			: this.renderSingleAction();
	}

	renderSingleAction() {
		return html` <uui-button
			class="action-button"
			.disabled=${this.disabled}
			label=${this.localize.term(`uSync_${this.button?.label}`)}
			color=${<UUIInterfaceColor>this.button?.color}
			look=${<UUIInterfaceLook>this.button?.look}
			state=${ifDefined(this.state)}
			@click=${() => this.#onClick(this.button)}></uui-button>`;
	}

	#onPopoverToggle(e: ToggleEvent) {
		this._popoverOpen = e.newState === 'open';
	}

	renderMultipleActions(parent?: SyncActionButton) {
		if (!this.button?.children) return nothing;

		const buttons = this.button?.children.map((item: SyncActionButton) => {
			return html` <uui-menu-item
				.disabled=${this.disabled}
				.label=${this.localize.term(`uSync_${item.label}`)}
				@click-label=${() => this.#onClick(item)}></uui-menu-item>`;
		});

		if (buttons.length == 0) return nothing;

		const popoverId = `popover_${parent?.key}`;

		return html`
			<uui-button-group class="action-button">
				<uui-button
					.disabled=${this.disabled}
					.label=${this.button.label}
					color=${<UUIInterfaceColor>parent?.color}
					look=${<UUIInterfaceLook>parent?.look}
					@click=${() => this.#onClick(this.button)}>
					${this.localize.term(`uSync_${this.button?.label}`)}
				</uui-button>
				<uui-button
					.disabled=${this.disabled}
					popovertarget=${popoverId}
					.label=${this.button.label}
					color=${<UUIInterfaceColor>parent?.color}
					look=${<UUIInterfaceLook>parent?.look}
					compact>
					<uui-symbol-expand
						class="expand-symbol"
						.open=${this._popoverOpen}></uui-symbol-expand>
				</uui-button>
			</uui-button-group>

			<uui-popover-container
				id=${popoverId}
				placement="bottom-end"
				@toggle=${this.#onPopoverToggle}>
				<umb-popover-layout>
					<uui-scroll-container> ${buttons} </uui-scroll-container>
				</umb-popover-layout>
			</uui-popover-container>
		`;
	}

	static styles = css`
		.action-button {
			min-width: 110px;
		}

		.expand-symbol {
			transform: rotate(90deg);
		}

		.expand-symbol[open] {
			transform: rotate(180deg);
		}
	`;
}

declare global {
	interface HTMLElementTagNameMap {
		'usync-action-button': SyncActionButtonElement;
	}
}
