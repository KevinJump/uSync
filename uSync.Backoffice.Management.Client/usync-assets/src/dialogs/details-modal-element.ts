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
	type USyncActionView,
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

	@state()
	private _item?: USyncActionView;

	@state()
	private _index = -1;

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
		this._item = this.data?.item;
		this._index = this.data?.items?.indexOf(this.data.item) ?? -1;
		this.#loadPreview();
	}

	#moveTo(index: number) {
		const item = this.data?.items?.[index];
		if (!item) return;

		this._index = index;
		this._item = item;
		this._preview = undefined;
		this.#loadPreview();
	}

	async #loadPreview() {
		const item = this._item;
		if (!item?.handler || !item.key) {
			this._tab = 'changes';
			return;
		}

		// only some handlers support previews, the rest return a 404
		// and the modal stays as it was (just the changes list).
		const { data } = await this.#repository.getPreview(item.handler, item.key);

		// moving through the list quickly can return previews out of order,
		// so drop anything that isn't for the item we are now showing.
		if (item !== this._item) return;

		if (data?.entityType) {
			this._preview = data;
		} else {
			this._tab = 'changes';
		}
	}

	#showActions() {
		const showActions = this.data?.showActions;
		if (typeof showActions === 'function') {
			return this._item ? showActions(this._item) : false;
		}
		return showActions ?? false;
	}

	async #onImport(e: Event) {
		e.stopPropagation();
		if (!this._item) return;

		const modalContext = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		if (!modalContext) return;

		const modal = modalContext.open(this, USYNC_IMPORT_SINGLE_MODAL, {
			data: { action: this._item },
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
			<umb-body-layout headline="Changes : ${this._item?.name ?? ''}">
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
									<usync-change-view .item=${this._item}></usync-change-view>
								</uui-box>`
					}
				</div>
				${this.renderNavigation()}
				<div slot="actions">
					<uui-button
						id="cancel"
						.label=${this.localize.term('general_close')}
						@click="${this.#onClose}"></uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	renderNavigation() {
		const count = this.data?.items?.length ?? 0;
		if (count < 2 || this._index < 0) return nothing;

		return html`<div slot="footer-info" id="navigation">
			<uui-button
				compact
				look="secondary"
				.label=${this.localize.term('general_previous')}
				?disabled=${this._index === 0}
				@click=${() => this.#moveTo(this._index - 1)}>
				<uui-icon name="icon-arrow-left"></uui-icon>
			</uui-button>
			<span>
				${this.localize.termOrDefault(
					'uSync_detailPosition',
					`${this._index + 1} of ${count}`,
					this.localize.number(this._index + 1),
					this.localize.number(count),
				)}
			</span>
			<uui-button
				compact
				look="secondary"
				.label=${this.localize.term('general_next')}
				?disabled=${this._index === count - 1}
				@click=${() => this.#moveTo(this._index + 1)}>
				<uui-icon name="icon-arrow-right"></uui-icon>
			</uui-button>
		</div>`;
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
		if (!this.#showActions()) return nothing;
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

		#navigation {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
			padding-left: var(--uui-size-space-4);
		}
	`;
}

export default uSyncDetailsModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-details-modal': uSyncDetailsModalElement;
	}
}
