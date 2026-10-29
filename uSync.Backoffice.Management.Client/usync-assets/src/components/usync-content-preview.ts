import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	type PropertyValues,
	classMap,
	css,
	customElement,
	html,
	nothing,
	property,
	repeat,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import '@umbraco-cms/backoffice/components';
import type {
	DataTypeResponseModel,
	DocumentResponseModel,
	MediaResponseModel,
	SyncContentPreviewInfo,
	SyncItemPreviewResponse,
} from '../api';
import {
	buildContentTypeLayout,
	type USyncLayoutProperty,
	type USyncLayoutTab,
	type USyncPreviewContentType,
} from '../utils/preview/content-type-layout.js';
import { deepEqual } from '../utils/diff/deep-equal.js';

type PreviewSide = 'current' | 'incoming';

type PreviewItem = DocumentResponseModel | MediaResponseModel;

interface PreviewSideData {
	item?: PreviewItem;
	info?: SyncContentPreviewInfo | null;
}

/** how a property's value differs between the two sides */
type ValueStatus = 'added' | 'removed' | 'changed';

/**
 * Shows the current and incoming versions of a content or media item side by
 * side, laid out the way the backoffice editor shows them, with every value
 * rendered by its real property editor. Values that change are highlighted.
 */
@customElement('usync-content-preview')
export class uSyncContentPreviewElement extends UmbLitElement {
	@property({ type: Object })
	preview?: SyncItemPreviewResponse;

	/** the tab shown on both sides (by name) */
	@state()
	private _activeTab?: string;

	/** the culture shown on both sides (undefined for invariant items) */
	@state()
	private _culture?: string;

	@state()
	private _onlyChanges = false;

	#current: PreviewSideData = {};
	#incoming: PreviewSideData = {};
	#type?: USyncPreviewContentType;
	#compositions = new Map<string, USyncPreviewContentType>();
	#dataTypes = new Map<string, DataTypeResponseModel>();
	#cultures: string[] = [];

	protected willUpdate(changed: PropertyValues<this>) {
		if (!changed.has('preview') || !this.preview) return;
		const preview = this.preview;

		const isMedia = preview.entityType === 'media';
		this.#current = {
			item: (isMedia ? preview.currentMedia : preview.currentDocument) ?? undefined,
			info: preview.currentInfo,
		};
		this.#incoming = {
			item: (isMedia ? preview.incomingMedia : preview.incomingDocument) ?? undefined,
			info: preview.incomingInfo,
		};

		this.#type =
			(isMedia ? preview.currentMediaType : preview.currentDocumentType) ?? undefined;
		const compositions: USyncPreviewContentType[] = isMedia
			? preview.compositionMediaTypes
			: preview.compositionDocumentTypes;
		this.#compositions = new Map(compositions.map((c) => [c.id, c]));
		this.#dataTypes = new Map(preview.dataTypes.map((d) => [d.id, d]));

		this.#cultures = [
			...new Set(
				[this.#current.item, this.#incoming.item]
					.flatMap((item) => item?.variants ?? [])
					.map((v) => v.culture)
					.filter((c): c is string => !!c),
			),
		].sort();

		this._culture = this.#cultures[0];
		this._activeTab = undefined;
	}

	render() {
		if (!this.preview) return nothing;

		const tabs = this.#type
			? this.#visibleTabs(buildContentTypeLayout(this.#type, this.#compositions))
			: [];
		// until a tab is picked, open on the first one with changes.
		const active =
			tabs.find((t) => t.name === this._activeTab) ??
			tabs.find((t) =>
				this.#tabProperties(t).some((p) => this.#valueStatus(p.property)),
			) ??
			tabs[0];

		return html`
			<div class="sides">
				${this.#renderToolbar()}
				${this.#renderSide('current', this.#current, tabs, active)}
				${this.#renderSide('incoming', this.#incoming, tabs, active)}
			</div>
		`;
	}

	#renderToolbar() {
		if (this.#cultures.length < 2 && !(this.#current.item && this.#incoming.item))
			return nothing;

		return html`<div class="toolbar">
			${
				this.#cultures.length > 1
					? html`<uui-button-group
							aria-label=${this.localize.termOrDefault('uSync_previewCulture', 'Language')}>
							${this.#cultures.map(
								(culture) =>
									html`<uui-button
										look=${culture === this._culture ? 'primary' : 'secondary'}
										compact
										label=${culture}
										@click=${() => (this._culture = culture)}></uui-button>`,
							)}
						</uui-button-group>`
					: nothing
			}
			${
				// there is only something to compare when the item is on both sides.
				this.#current.item && this.#incoming.item
					? html`<uui-toggle
							label=${this.localize.termOrDefault('uSync_previewOnlyChanges', 'Only show changes')}
							?checked=${this._onlyChanges}
							@change=${(e: Event) =>
								(this._onlyChanges = (
									e.target as HTMLInputElement
								).checked)}></uui-toggle>`
					: nothing
			}
		</div>`;
	}

	#renderSide(
		side: PreviewSide,
		data: PreviewSideData,
		tabs: USyncLayoutTab[],
		active: USyncLayoutTab | undefined,
	) {
		const headline =
			side === 'current'
				? this.localize.termOrDefault('uSync_previewCurrent', 'Current (this site)')
				: this.localize.termOrDefault('uSync_previewIncoming', 'Incoming (uSync files)');

		if (!data.item) {
			return html`<div class="side ${side}">
				<h4 class="side-headline">${headline}</h4>
				<uui-box class="info"><p class="empty">${this.#missingMessage(side)}</p></uui-box>
			</div>`;
		}

		return html`
			<div class="side ${side}">
				<h4 class="side-headline">${headline}</h4>
				${this.#renderInfo(data)}
				${
					tabs.length > 1 || (tabs.length === 1 && tabs[0].name)
						? html`<uui-tab-group>
								${tabs.map((tab) => this.#renderTabButton(tab, tab === active))}
							</uui-tab-group>`
						: html`<div></div>`
				}
				${
					active
						? this.#renderTab(data.item, active)
						: html`<uui-box>
								<p class="empty">
									${
										this._onlyChanges
											? this.localize.termOrDefault(
													'uSync_previewNoValueChanges',
													'No values change',
												)
											: this.localize.termOrDefault(
													'uSync_previewNoProperties',
													'No properties',
												)
									}
								</p>
							</uui-box>`
				}
			</div>
		`;
	}

	#missingMessage(side: PreviewSide) {
		if (side === 'current') {
			return this.localize.termOrDefault(
				'uSync_previewNotInSite',
				'This item does not exist in the site yet',
			);
		}
		return this.preview?.isDelete
			? this.localize.termOrDefault(
					'uSync_previewDeleted',
					'This item will be deleted or renamed by the import',
				)
			: this.localize.termOrDefault(
					'uSync_previewFailed',
					'The incoming item could not be built for the preview',
				);
	}

	#renderInfo(data: PreviewSideData) {
		const item = data.item!;
		const info = data.info;
		const other = data === this.#current ? this.#incoming : this.#current;
		const both = !!other.item;

		const changed = (field: keyof SyncContentPreviewInfo | 'name') => {
			if (!both) return { changed: false };
			const mine = field === 'name' ? this.#name(item) : info?.[field];
			const theirs = field === 'name' ? this.#name(other.item!) : other.info?.[field];
			return { changed: !deepEqual(mine, theirs) };
		};

		const icon = 'documentType' in item ? item.documentType.icon : item.mediaType.icon;

		return html`
			<uui-box class="info">
				<div slot="headline" class="headline">
					<umb-icon name=${icon}></umb-icon>
					<span class=${classMap(changed('name'))}>${this.#name(item)}</span>
				</div>
				<dl class="meta">
					<dt>${this.localize.termOrDefault('uSync_previewParent', 'Parent')}</dt>
					<dd>
						<span class=${classMap(changed('parent'))}
							>${info?.parent ?? this.localize.termOrDefault('uSync_previewRoot', '(root)')}</span
						>
					</dd>
					${
						info?.template !== undefined && this.preview?.entityType === 'document'
							? html`<dt>
										${this.localize.termOrDefault('uSync_previewTemplate', 'Template')}
									</dt>
									<dd>
										<span class=${classMap(changed('template'))}
											>${info?.template ?? '-'}</span
										>
									</dd>`
							: nothing
					}
					<dt>${this.localize.termOrDefault('uSync_previewSortOrder', 'Sort order')}</dt>
					<dd>
						<span class=${classMap(changed('sortOrder'))}>${info?.sortOrder ?? '-'}</span>
					</dd>
					${
						info?.published.length
							? html`<dt>
										${this.localize.termOrDefault('uSync_previewPublished', 'Published')}
									</dt>
									<dd>
										<span class=${classMap({ tags: true, ...changed('published') })}>
											${info.published.map((p) => this.#renderPublished(p.culture, p.published))}
										</span>
									</dd>`
							: nothing
					}
				</dl>
				${
					info?.trashed
						? html`<uui-tag color="danger" look="secondary"
								>${this.localize.termOrDefault('uSync_previewTrashed', 'In the recycle bin')}</uui-tag
							>`
						: nothing
				}
			</uui-box>
		`;
	}

	#renderPublished(culture: string, published: boolean) {
		const label = published
			? this.localize.termOrDefault('uSync_previewPublished', 'Published')
			: this.localize.termOrDefault('uSync_previewNotPublished', 'Not published');
		return html`<uui-tag
			size="s"
			look=${published ? 'primary' : 'secondary'}
			color=${published ? 'positive' : 'default'}
			>${culture ? `${culture}: ` : ''}${label}</uui-tag
		>`;
	}

	/** the item's name in the culture being shown */
	#name(item: PreviewItem) {
		const variant =
			item.variants.find((v) => (v.culture ?? undefined) === this._culture) ??
			item.variants[0];
		return variant?.name ?? '';
	}

	#renderTabButton(tab: USyncLayoutTab, active: boolean) {
		const changed = this.#tabProperties(tab).some((p) => this.#valueStatus(p.property));
		const label =
			tab.name || this.localize.termOrDefault('uSync_previewGenericTab', 'Generic');
		return html`<uui-tab
			label=${label}
			?active=${active}
			@click=${() => (this._activeTab = tab.name)}>
			<span class="tab-label">
				${label} ${changed ? html`<span class="dot changed"></span>` : nothing}
			</span>
		</uui-tab>`;
	}

	#renderTab(item: PreviewItem, tab: USyncLayoutTab) {
		// inert rather than readonly: editors are shown as an editor would see them,
		// but nothing in the preview can be changed.
		return html`
			<umb-property-dataset .value=${this.#datasetValue(item)} inert>
				${tab.properties.length ? html`<uui-box>${this.#renderProperties(tab.properties)}</uui-box>` : nothing}
				${repeat(
					tab.groups,
					(group) => group.name,
					(group) =>
						html`<uui-box headline=${group.name}
							>${this.#renderProperties(group.properties)}</uui-box
						>`,
				)}
			</umb-property-dataset>
		`;
	}

	#renderProperties(properties: USyncLayoutProperty[]) {
		return repeat(
			properties,
			(entry) => entry.property.id,
			({ property: p }) => {
				const dataType = this.#dataTypes.get(p.dataType.id);
				const status = this.#valueStatus(p);

				return html`<div class="property ${status ?? ''}">
					${
						dataType
							? html`<umb-property
									.alias=${p.alias}
									.label=${p.name}
									.description=${p.description ?? ''}
									.propertyEditorUiAlias=${dataType.editorUiAlias}
									.config=${dataType.values}
									.appearance=${p.appearance}></umb-property>`
							: html`<p class="empty">${p.name}</p>`
					}
				</div>`;
			},
		);
	}

	/** tabs and groups with the properties to show (all, or only the ones that change) */
	#visibleTabs(tabs: USyncLayoutTab[]) {
		if (!this._onlyChanges) return tabs;

		const keep = (properties: USyncLayoutProperty[]) =>
			properties.filter((p) => this.#valueStatus(p.property));

		return tabs
			.map((tab) => ({
				...tab,
				properties: keep(tab.properties),
				groups: tab.groups
					.map((group) => ({ ...group, properties: keep(group.properties) }))
					.filter((group) => group.properties.length > 0),
			}))
			.filter((tab) => tab.properties.length > 0 || tab.groups.length > 0);
	}

	#tabProperties(tab: USyncLayoutTab) {
		return [...tab.properties, ...tab.groups.flatMap((g) => g.properties)];
	}

	/** the values the editors on one side read, for the culture being shown */
	#datasetValue(item: PreviewItem) {
		return item.values
			.filter((v) => !v.segment && (!v.culture || v.culture === this._culture))
			.map((v) => ({ alias: v.alias, value: v.value }));
	}

	#value(item: PreviewItem | undefined, p: USyncLayoutProperty['property']) {
		const culture = p.variesByCulture ? this._culture : undefined;
		return item?.values.find(
			(v) => v.alias === p.alias && !v.segment && (v.culture ?? undefined) === culture,
		)?.value;
	}

	#valueStatus(p: USyncLayoutProperty['property']): ValueStatus | undefined {
		const current = this.#current.item;
		const incoming = this.#incoming.item;
		if (!current || !incoming) return undefined;

		const before = this.#value(current, p);
		const after = this.#value(incoming, p);
		if (deepEqual(normalise(before), normalise(after))) return undefined;
		if (isEmpty(before)) return 'added';
		if (isEmpty(after)) return 'removed';
		return 'changed';
	}

	static styles = css`
		:host {
			display: block;
			container-type: inline-size;
		}

		/* the toolbar shares the headline row with the incoming side, on the right. */
		.toolbar {
			grid-column: 2;
			grid-row: 1;
			justify-self: end;
			align-self: center;
			z-index: 1;
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-4);
		}

		/* the two sides share rows (headline, info, tabs, content), so the
		   tab bars and properties line up even when the info boxes differ. */
		.sides {
			display: grid;
			grid-template-columns: 1fr 1fr;
			grid-template-rows: auto auto auto 1fr;
			gap: var(--uui-size-space-4) var(--uui-size-space-5);
		}

		.side {
			display: grid;
			grid-row: span 4;
			grid-template-rows: subgrid;
			align-items: start;
			min-width: 0;
		}

		.side.current {
			grid-column: 1;
			grid-row: 1 / span 4;
		}

		.side.incoming {
			grid-column: 2;
			grid-row: 1 / span 4;
		}

		.side > .info:not(:last-child) {
			align-self: stretch;
		}

		@container (max-width: 760px) {
			.sides {
				grid-template-columns: 1fr;
				grid-template-rows: none;
				row-gap: var(--uui-size-layout-1);
			}

			.side,
			.side.current,
			.side.incoming,
			.toolbar {
				grid-column: 1;
				grid-row: auto;
			}

			.side {
				display: flex;
				flex-direction: column;
				align-items: stretch;
				gap: var(--uui-size-space-4);
			}

			.side > .side-headline {
				align-self: auto;
			}
		}

		umb-property-dataset {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
			min-width: 0;
		}

		.side-headline {
			align-self: center;
			margin: 0;
			font-size: var(--uui-type-small-size);
			text-transform: uppercase;
			letter-spacing: 0.04em;
			color: var(--uui-color-text-alt);
		}

		.headline {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
		}

		.meta {
			display: grid;
			grid-template-columns: max-content 1fr;
			align-items: center;
			gap: var(--uui-size-space-2) var(--uui-size-space-5);
			margin: 0 0 var(--uui-size-space-2);
		}

		.meta dt {
			font-weight: 700;
		}

		.tags {
			display: inline-flex;
			flex-wrap: wrap;
			gap: var(--uui-size-space-2);
		}

		.meta dd {
			display: flex;
			flex-wrap: wrap;
			gap: var(--uui-size-space-2);
			margin: 0;
		}

		/* a changed field gets a soft highlight, spread with a shadow so the text doesn't move. */
		.changed:not(.property):not(.dot) {
			background: color-mix(in srgb, var(--uui-color-warning) 35%, transparent);
			border-radius: var(--uui-border-radius);
			box-shadow: 0 0 0 var(--uui-size-space-1)
				color-mix(in srgb, var(--uui-color-warning) 35%, transparent);
		}

		uui-tab-group {
			--uui-tab-divider: var(--uui-color-border);
			border-bottom: 1px solid var(--uui-color-border);
		}

		.tab-label {
			display: inline-flex;
			align-items: center;
			gap: var(--uui-size-space-2);
			white-space: nowrap;
		}

		.dot {
			display: inline-block;
			width: 8px;
			height: 8px;
			border-radius: 50%;
		}

		.dot.changed {
			background: var(--uui-color-warning);
		}

		/* marked with an inset bar rather than a border, so the content doesn't shift. */
		.property.added {
			box-shadow: inset 3px 0 0 var(--uui-color-positive);
			background: color-mix(in srgb, var(--uui-color-positive) 6%, transparent);
		}

		.property.removed {
			box-shadow: inset 3px 0 0 var(--uui-color-danger);
			background: color-mix(in srgb, var(--uui-color-danger) 6%, transparent);
		}

		.property.changed {
			box-shadow: inset 3px 0 0 var(--uui-color-warning);
			background: color-mix(in srgb, var(--uui-color-warning) 10%, transparent);
		}

		/* properties run edge to edge in their box, like the backoffice editor. */
		.property {
			padding: 0 var(--uui-size-space-5);
			margin: 0 calc(-1 * var(--uui-size-space-5));
		}

		.property:first-child {
			margin-top: calc(-1 * var(--uui-size-space-5));
		}

		.property:last-child {
			margin-bottom: calc(-1 * var(--uui-size-space-5));
		}

		.property + .property {
			border-top: 1px solid var(--uui-color-divider);
		}

		/* umb-property pads itself for a full workspace, tighten it up here. */
		umb-property {
			--uui-size-layout-1: var(--uui-size-space-4);
		}

		.empty {
			margin: 0;
			color: var(--uui-color-text-alt);
		}
	`;
}

/** values that mean "nothing set" compare as equal */
function normalise(value: unknown) {
	return isEmpty(value) ? undefined : value;
}

function isEmpty(value: unknown) {
	return (
		value === undefined ||
		value === null ||
		value === '' ||
		(Array.isArray(value) && value.length === 0)
	);
}

export default uSyncContentPreviewElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-content-preview': uSyncContentPreviewElement;
	}
}
