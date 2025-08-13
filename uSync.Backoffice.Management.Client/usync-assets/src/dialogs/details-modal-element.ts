import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { css, customElement, html } from '@umbraco-cms/backoffice/external/lit';
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
		console.log(this.data);
		return html`
			<umb-body-layout headline="Changes : ${this.data?.item.name ?? ''}">
				<uui-box style="--uui-box-default-padding: 0;">
					<div slot="header" id="header">
						<h3><umb-localize key="uSync_detailHeadline"></umb-localize></h3>
						<umb-localize key="uSync_detailHeader"></umb-localize>
					</div>
				</uui-box>
				<uui-box style="--uui-box-default-padding: 0;">
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

	static styles = css`
		#header h3 {
			margin: 0;
		}
	`;
}

export default uSyncDetailsModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-details-modal': uSyncDetailsModalElement;
	}
}
