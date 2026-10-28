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
	uSyncActionRepository,
	uSyncDetailsModalData,
	uSyncDetailsModalValue,
	type SyncItemPreviewResponse,
} from '@jumoo/uSync';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import '../components/usync-data-type-preview.js';
import '../components/usync-content-type-preview.js';
import '../components/usync-content-preview.js';

type DetailsTab = 'changes' | 'preview';

@customElement('usync-details-modal')
export class uSyncDetailsModalElement extends UmbModalBaseElement<
	uSyncDetailsModalData,
	uSyncDetailsModalValue
> {
	@state()
	importState: UUIButtonState;

	@state()
	private _preview?: SyncItemPreviewResponse;

	@state()
	private _tab: DetailsTab = 'changes';

	#repository = new uSyncActionRepository(this);

	constructor() {
		super();
		// this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
		// 	if (!_instance) return;
		// 	this.#actionContext = _instance;
		// });
	}

	connectedCallback() {
		super.connectedCallback();
		this.#loadPreview();
	}

	async #loadPreview() {
		const item = this.data?.item;
		if (!item?.handler || !item.key) return;

		// only some handlers support previews, the rest return a 404
		// and the modal stays as it was (just the changes list).
		const { data } = await this.#repository.getPreview(item.handler, item.key);
		if (data?.entityType) {
			this._preview = data;
		}
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
				<div class="layout">
					<uui-box style="--uui-box-default-padding: 0;">
						<div slot="header" id="header">
							<h3><umb-localize key="uSync_detailHeadline"></umb-localize></h3>
							<umb-localize key="uSync_detailHeader"></umb-localize>
						</div>
						<div slot="header-actions">${this.renderActions()}</div>
					</uui-box>
					${this.renderTabs()}
					${
						this._tab === 'preview' && this._preview
							? this.renderPreview(this._preview)
							: html`<uui-box style="--uui-box-default-padding: 0;">
									<usync-change-view .item=${this.data?.item}></usync-change-view>
								</uui-box>`
					}
				</div>
				<div slot="actions">
					<uui-button
						id="cancel"
						.label=${this.localize.term('general_close')}
						@click="${this.#onClose}"></uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	renderTabs() {
		if (!this._preview) return nothing;

		return html`<uui-tab-group>
			<uui-tab
				.label=${this.localize.termOrDefault('uSync_changesTab', 'Changes')}
				?active=${this._tab === 'changes'}
				@click=${() => (this._tab = 'changes')}></uui-tab>
			<uui-tab
				.label=${this.localize.termOrDefault('uSync_previewTab', 'Preview')}
				?active=${this._tab === 'preview'}
				@click=${() => (this._tab = 'preview')}></uui-tab>
		</uui-tab-group>`;
	}

	renderPreview(preview: SyncItemPreviewResponse) {
		switch (preview.entityType) {
			case 'data-type':
				return html`<usync-data-type-preview
					.preview=${preview}></usync-data-type-preview>`;
			case 'document-type':
			case 'media-type':
			case 'member-type':
				return html`<usync-content-type-preview
					.preview=${preview}></usync-content-type-preview>`;
			case 'document':
			case 'media':
				return html`<usync-content-preview
					.preview=${preview}></usync-content-preview>`;
			default:
				return nothing;
		}
	}

	renderActions() {
		if (!this.data?.showActions) return nothing;
		return html` <uui-button
			id="import"
			type="button"
			look="outline"
			.state=${this.importState}
			.label=${this.localize.termOrDefault('uSync_importSingle', 'Import')}
			@click="${this.#onImport}"></uui-button>`;
	}

	static styles = css`
		.layout {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
		}

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
