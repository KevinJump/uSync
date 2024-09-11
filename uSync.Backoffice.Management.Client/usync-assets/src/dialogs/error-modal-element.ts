import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { uSyncErrorModalData, uSyncErrorModalValue } from './error-modal-token';
import { css, customElement, html } from '@umbraco-cms/backoffice/external/lit';

@customElement('usync-error-modal')
export default class uSyncErrorModalElement extends UmbModalBaseElement<
	uSyncErrorModalData,
	uSyncErrorModalValue
> {
	#onClose() {
		this.modalContext?.reject();
	}

	render() {
		const headline = `Error: ${this.data?.action.name ?? ''} [${this.data?.action.itemType}]`;

		return html`<umb-body-layout .headline=${headline}>
			<strong>
				<umb-localize key="uSync_errorHeader"></umb-localize>
			</strong>
			<div class="error">${this.data?.action.message}</div>
			<div slot="actions">
				<uui-button
					id="cancel"
					.label=${this.localize.term('general_close')}
					@click="${this.#onClose}"></uui-button>
			</div>
		</umb-body-layout>`;
	}

	static styles = css`
		umb-body-layout {
			max-width: 450px;
		}

		.error {
			padding: 10px;
			font-family: monospace;
			color: red;
		}
	`;
}
