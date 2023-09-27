import { LitElement, html } from "lit";
import { customElement } from "lit/decorators.js";
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UmbNotificationContext, UMB_NOTIFICATION_CONTEXT_TOKEN } from '@umbraco-cms/backoffice/notification';

@customElement('my-element')
export default class MyElement extends UmbElementMixin(LitElement) {
	#notificationContext?: UmbNotificationContext;

	constructor() {
		super();
		this.consumeContext(UMB_NOTIFICATION_CONTEXT_TOKEN, (_instance) => {
			this.#notificationContext = _instance;
		});
	}

	#onClick = () => {
		this.#notificationContext?.peek('positive', { data: { message: '#h5yr' } });
	}

	render() {
		return html`
			<uui-box headline="Welcome">
				<p>A TypeScript Lit Dashboard</p>
				<uui-button look="primary" label="Click me" @click=${this.#onClick}></uui-button>
			</uui-box>
		`;
	}
}

declare global {
	interface HTMLElementTagNameMap {
		'my-element': MyElement;
	}
}