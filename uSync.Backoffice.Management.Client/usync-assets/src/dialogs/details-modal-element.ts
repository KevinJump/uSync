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
	type ManifestuSyncItemPreview,
	type SyncItemPreviewResponse,
	type USyncActionView,
} from '@jumoo/uSync';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';

type DetailsTab = 'changes' | 'diff';

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
	private _loading = false;

	@state()
	private _item?: USyncActionView;

	@state()
	private _index = -1;

	/** the entity types there is a 'usync-item-preview' element for */
	@state()
	private _previewTypes = new Set<string>();

	#repository = new uSyncActionRepository(this);

	// kept between renders, so the extension slot doesn't rebuild the preview each time.
	#previewFilter = (_manifest: ManifestuSyncItemPreview) => false;
	#previewProps: { preview?: SyncItemPreviewResponse } = {};

	constructor() {
		super();

		this.observe(umbExtensionsRegistry.byType('usync-item-preview'), (manifests) => {
			this._previewTypes = new Set(manifests.flatMap((m) => m.forEntityTypes));
		});

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
		if (!item?.handler || !item.key) return;

		// only some handlers support previews, the rest return a 404
		// and the modal shows the diff on its own. we leave _tab alone
		// so a user who picked the diff stays on it as they move through.
		this._loading = true;
		const { data } = await this.#repository.getPreview(item.handler, item.key);

		// moving through the list quickly can return previews out of order,
		// so drop anything that isn't for the item we are now showing.
		if (item !== this._item) return;

		this._loading = false;
		if (data?.entityType) {
			const entityType = data.entityType;
			this.#previewFilter = (manifest) => manifest.forEntityTypes.includes(entityType);
			this.#previewProps = { preview: data };
			this._preview = data;
		}
	}

	/** there is a preview, and an element registered to show it */
	#hasPreview() {
		return !!this._preview && this._previewTypes.has(this._preview.entityType);
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
			<umb-body-layout>
				${this.renderHeadline()}
				<div class="layout">
					<uui-box style="--uui-box-default-padding: 0;">
						<div slot="header" id="header">
							<h3><umb-localize key="uSync_detailHeadline"></umb-localize></h3>
							<umb-localize key="uSync_detailHeader"></umb-localize>
						</div>
						<div slot="header-actions">${this.renderActions()}</div>
					</uui-box>
					${this.renderTabs()}
					${this.renderTabContent()}
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

	renderHeadline() {
		const item = this._item;
		if (!item) return nothing;

		// singular names first (Content type), then the plural group
		// names the results use (Content Types), then the raw type.
		const itemType = this.localize.termOrDefault(
			`uSync_itemType${item.itemType}`,
			this.localize.termOrDefault(`uSync_${item.itemType}`, item.itemType),
		);

		return html`<div slot="header" id="headline">
			<h3 title=${item.name}>Changes : ${item.name}</h3>
			<span class="item-info">${itemType} · ${item.change}</span>
		</div>`;
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
		if (!this.#hasPreview()) return nothing;

		return html`<uui-tab-group>
			${this.renderTab('changes', 'uSync_changesTab', 'Changes', 'icon-eye')}
			${this.renderTab('diff', 'uSync_diffTab', 'Diff', 'icon-code')}
		</uui-tab-group>`;
	}

	renderTab(tab: DetailsTab, key: string, fallback: string, icon: string) {
		const label = this.localize.termOrDefault(key, fallback);
		return html`<uui-tab
			.label=${label}
			?active=${this._tab === tab}
			@click=${() => (this._tab = tab)}>
			<!-- uui-tab stacks its icon slot above the label, so the icon
			     goes in the label to keep them on one line. -->
			<span class="tab-label"><umb-icon name=${icon}></umb-icon>${label}</span>
		</uui-tab>`;
	}

	renderTabContent() {
		// the visual changes view is the default, the raw diff is there
		// for anything that doesn't have a preview (or if you ask for it).
		if (this._tab === 'changes') {
			if (this._loading) return html`<uui-loader-bar></uui-loader-bar>`;
			if (this.#hasPreview()) return this.renderPreview();
		}

		return html`<uui-box style="--uui-box-default-padding: 0;">
			<usync-change-view .item=${this._item}></usync-change-view>
		</uui-box>`;
	}

	renderPreview() {
		// packages can show previews for their own items by registering a
		// 'usync-item-preview' element for the entity type their preview mapper returns.
		return html`<umb-extension-slot
			type="usync-item-preview"
			single
			.filter=${this.#previewFilter}
			.props=${this.#previewProps}></umb-extension-slot>`;
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

		#headline {
			display: flex;
			flex-direction: column;
			min-width: 0;
		}

		#headline h3 {
			margin: 0;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		#headline .item-info {
			font-size: var(--uui-type-small-size);
			color: var(--uui-color-text-alt);
		}

		uui-tab-group {
			background-color: var(--uui-color-surface);
			border-radius: var(--uui-border-radius);
			box-shadow: var(--uui-shadow-depth-1);
			padding: 0 var(--uui-size-space-2);
		}

		.tab-label {
			display: inline-flex;
			align-items: center;
			gap: var(--uui-size-space-2);
		}

		.tab-label umb-icon {
			font-size: 16px;
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
