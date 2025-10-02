import {
	UMB_MODAL_MANAGER_CONTEXT,
	UmbModalBaseElement,
} from '@umbraco-cms/backoffice/modal';
import {
	css,
	customElement,
	html,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import {
	USYNC_IMPORT_SINGLE_MODAL,
	uSyncDetailsModalData,
	uSyncDetailsModalValue,
} from '@jumoo/uSync';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';

@customElement('usync-details-modal')
export class uSyncDetailsModalElement extends UmbModalBaseElement<
	uSyncDetailsModalData,
	uSyncDetailsModalValue
> {
	@state()
	importState: UUIButtonState;

	constructor() {
		super();
		// this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
		// 	if (!_instance) return;
		// 	this.#actionContext = _instance;
		// });
	}

	async #onImport(e: Event) {
		e.stopPropagation();
		if (!this.data?.item) return;

		const modalContext = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalContext) return;

		const modal = modalContext.open(this, USYNC_IMPORT_SINGLE_MODAL, {
			data: { action: this.data?.item },
		});

		const data = await modal?.onSubmit().catch(() => {
			return;
		});

		return data;
	}

	#onClose() {
		this.modalContext?.reject();
	}

	render() {
		return html`
			<umb-body-layout headline="Changes : ${this.data?.item.name ?? ''}">
				<uui-box style="--uui-box-default-padding: 0;">
					<div slot="header" id="header">
						<h3><umb-localize key="uSync_detailHeadline"></umb-localize></h3>
						<umb-localize key="uSync_detailHeader"></umb-localize>
					</div>
					<div slot="header-actions">${this.renderActions()}</div>
				</uui-box>
				<uui-box>
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

	renderActions() {
		if (!this.data?.showActions) return nothing;
		return html` <uui-button
			id="import"
			type="button"
			look="outline"
			.state=${this.importState}
			.label=${this.localize.term('uSync_importSingle')}
			@click="${this.#onImport}"></uui-button>`;
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
