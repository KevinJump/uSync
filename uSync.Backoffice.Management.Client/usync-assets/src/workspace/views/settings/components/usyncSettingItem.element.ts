import { LitElement, classMap, css, customElement, html, property } from '@umbraco-cms/backoffice/external/lit';

@customElement('usync-setting-item')
export class uSyncSettingItemElement extends LitElement {
	@property({ type: String })
	name?: string;

	@property({ type: String })
	description?: string;

	@property()
	value?: Object | null | undefined;

	#renderValue() {
		if (this.value === undefined || this.value === null) {
			return html`(Not Set)`;
		}

		if (typeof this.value == 'boolean') {
			const classes = { _set: this.value };

			return html` <uui-icon name=${this.value ? 'icon-check' : 'icon-wrong'} class=${classMap(classes)}></uui-icon> `;
		} else {
			if (Array.isArray(this.value)) {
				console.log('Array', this.value);

				const list = this.value.map((v) => {
					return html`<li>${v}</li>`;
				});

				return html`<ul>
					${list}
				</ul>`;
			} else {
				return html` <div>${this.value}</div> `;
			}
		}
	}

	render() {
		return html`
			<div class="usync-setting-value">
				<div class="info">
					<h5>${this.name}</h5>
					<div>${this.description}</div>
				</div>
				<div class="value">${this.#renderValue()}</div>
			</div>
		`;
	}

	static styles = css`
		.usync-setting-value {
			display: flex;
			justify-content: space-between;
			padding: 10px 0;
			border-bottom: 1px solid var(--uui-color-divider);
		}

		.usync-setting-value h5 {
			margin: 0;
			padding: 0;
		}

		uui-icon {
			color: red;
		}

		uui-icon._set {
			color: green;
		}

		ul {
			list-style: none;
		}
	`;
}

export default uSyncSettingItemElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-setting-item': uSyncSettingItemElement;
	}
}
