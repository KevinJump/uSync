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

		this.dispatchEvent(
			new CustomEvent('usync-action-click', {
				detail: {
					button: item,
				},
			}),
		);
	}

	render() {
		return html`
			<uui-button-group>
				<uui-button
					.disabled=${this.disabled}
					label=${this.localize.term(`uSync_${this.button?.label}`)}
					color=${<UUIInterfaceColor>this.button?.color}
					look=${<UUIInterfaceLook>this.button?.look}
					state=${ifDefined(this.state)}
					@click=${() => this.#onClick(this.button)}></uui-button>

				${this.renderDropdown(this.button)}
			</uui-button-group>
		`;
	}

	#onPopoverToggle(e: ToggleEvent) {
		this._popoverOpen = e.newState === 'open';
	}

	renderDropdown(parent?: SyncActionButton) {
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

			<uui-popover-container
				id=${popoverId}
				margin="6"
				placement="bottom-end"
				@toggle=${this.#onPopoverToggle}>
				<umb-popover-layout>
					<uui-scroll-container> ${buttons} </uui-scroll-container>
				</umb-popover-layout>
			</uui-popover-container>
		`;
	}

	static styles = css`
		// TODO: Same hack as in the core, should use proper symbol (when one is avalible.)
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
