import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { customElement, html } from '@umbraco-cms/backoffice/external/lit';
import { uSyncDetailsModalData, uSyncDetailsModalValue } from '@jumoo/uSync';

@customElement('usync-details-modal')
export class uSyncDetailsModalElement extends UmbModalBaseElement<
	uSyncDetailsModalData,
	uSyncDetailsModalValue
> {
	#onClose() {
		this.modalContext?.reject();
	}

	render() {
		return html`
			<umb-body-layout headline="Changes : ${this.data?.item.name ?? ''}">
				<uui-box .headline=${this.localize.term('uSync_detailHeadline')}>
					<div slot="header">
						<umb-localize key="uSync_detailHeader"></umb-localize>
					</div>
					<usync-change-view .item=${this.data?.item}></usync-change-view>
				</uui-box>
				<div slot="actions">
					<uui-button
						id="cancel"
						.label=${this.localize.term('general_close')}
						@click="${this.#onClose}"></uui-button>
				</div>
			</umb-body-layout>
		`;
	}
}

export default uSyncDetailsModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-details-modal': uSyncDetailsModalElement;
	}
}
