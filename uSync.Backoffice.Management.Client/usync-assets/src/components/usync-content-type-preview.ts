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
import type { DataTypeResponseModel, SyncItemPreviewResponse } from '../api';
import {
	buildContentTypeLayout,
	compositionIds,
	type USyncLayoutProperty,
	type USyncLayoutTab,
	type USyncPreviewContentType,
} from '../utils/preview/content-type-layout.js';
import {
	diffContentTypes,
	type USyncContentTypeDiff,
	type USyncPreviewItemChange,
} from '../utils/preview/content-type-diff.js';

type PreviewSide = 'current' | 'incoming';

interface PreviewSides {
	current?: USyncPreviewContentType;
	incoming?: USyncPreviewContentType;
	compositions: USyncPreviewContentType[];
}

const KIND_LABELS: Record<string, [string, string]> = {
	'document-type': ['uSync_previewDocumentType', 'Document type'],
	'media-type': ['uSync_previewMediaType', 'Media type'],
	'member-type': ['uSync_previewMemberType', 'Member type'],
};

/**
 * Shows the current and incoming versions of a document, media or member
 * type side by side, laid out the way the backoffice editor shows them, with
 * every property rendered by its real property editor. Added, removed and
 * changed tabs, groups and properties are highlighted.
 */
@customElement('usync-content-type-preview')
export class uSyncContentTypePreviewElement extends UmbLitElement {
	@property({ type: Object })
	preview?: SyncItemPreviewResponse;

	/** the tab shown on both sides (by name, so it lines up when ids differ) */
	@state()
	private _activeTab?: string;

	#sides: PreviewSides = { compositions: [] };
	#compositions = new Map<string, USyncPreviewContentType>();
	#dataTypes = new Map<string, DataTypeResponseModel>();
	#newDataTypes = new Set<string>();
	#newCompositions = new Set<string>();
	#diff?: USyncContentTypeDiff;

	protected willUpdate(changed: PropertyValues<this>) {
		if (!changed.has('preview') || !this.preview) return;

		this.#sides = getSides(this.preview);
		this.#compositions = new Map(this.#sides.compositions.map((c) => [c.id, c]));
		this.#dataTypes = new Map(this.preview.dataTypes.map((d) => [d.id, d]));
		this.#newDataTypes = new Set(this.preview.newDataTypes);
		this.#newCompositions = new Set(this.preview.newCompositions);
		this.#diff = diffContentTypes(this.#sides.current, this.#sides.incoming);
		this._activeTab = undefined;
	}

	render() {
		if (!this.preview) return nothing;

		return html`
			<div class="sides">
				${this.#renderSide('current', this.#sides.current)}
				${this.#renderSide('incoming', this.#sides.incoming)}
			</div>
		`;
	}

	#renderSide(side: PreviewSide, type: USyncPreviewContentType | undefined) {
		const headline =
			side === 'current'
				? this.localize.termOrDefault('uSync_previewCurrent', 'Current (this site)')
				: this.localize.termOrDefault('uSync_previewIncoming', 'Incoming (uSync files)');

		if (!type) {
			return html`<div class="side">
				<h4 class="side-headline">${headline}</h4>
				<uui-box class="info"><p class="empty">${this.#missingMessage(side)}</p></uui-box>
			</div>`;
		}

		const tabs = buildContentTypeLayout(type, this.#compositions);
		const active = tabs.find((t) => t.name === this._activeTab) ?? tabs[0];

		return html`
			<div class="side">
				<h4 class="side-headline">${headline}</h4>
				${this.#renderInfo(type)}
				${
					tabs.length > 1 || (tabs.length === 1 && tabs[0].name)
						? html`<uui-tab-group>
								${tabs.map((tab) => this.#renderTabButton(tab, tab === active))}
							</uui-tab-group>`
						: html`<div></div>`
				}
				${
					active
						? this.#renderTab(active)
						: html`<uui-box>
								<p class="empty">
									${this.localize.termOrDefault('uSync_previewNoProperties', 'This type has no properties')}
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

	#renderInfo(type: USyncPreviewContentType) {
		const changed = (field: string) => ({
			changed: this.#diff?.fields.has(field) ?? false,
		});
		const [kindKey, kindDefault] = KIND_LABELS[this.preview?.entityType ?? ''] ?? [
			'',
			'',
		];
		const compositions = compositionIds(type).map((id) => ({
			name: this.#compositions.get(id)?.name ?? id,
			isNew: this.#newCompositions.has(id),
		}));

		return html`
			<uui-box class="info">
				<div slot="headline" class="headline">
					<umb-icon
						name=${type.icon.split(' ')[0]}
						class=${classMap(changed('icon'))}></umb-icon>
					<span class=${classMap(changed('name'))}>${type.name}</span>
					<code class=${classMap(changed('alias'))}>${type.alias}</code>
				</div>
				<div class="info-body">
					<div class="tags">
						<uui-tag look="secondary" class=${classMap(changed('isElement'))}>
							${
								type.isElement
									? this.localize.termOrDefault(
											'uSync_previewElementType',
											'Element type',
										)
									: this.localize.termOrDefault(kindKey, kindDefault)
							}
						</uui-tag>
						${
							type.allowedAsRoot
								? html`<uui-tag
										look="secondary"
										class=${classMap(changed('allowedAsRoot'))}>
										${this.localize.termOrDefault('uSync_previewAllowedAtRoot', 'Allowed at root')}
									</uui-tag>`
								: nothing
						}
						${
							type.variesByCulture
								? html`<uui-tag
										look="secondary"
										class=${classMap(changed('variesByCulture'))}>
										${this.localize.termOrDefault('uSync_previewVariesByCulture', 'Varies by culture')}
									</uui-tag>`
								: nothing
						}
						${
							type.variesBySegment
								? html`<uui-tag
										look="secondary"
										class=${classMap(changed('variesBySegment'))}>
										${this.localize.termOrDefault('uSync_previewVariesBySegment', 'Varies by segment')}
									</uui-tag>`
								: nothing
						}
					</div>
					${
						type.description
							? html`<p
									class=${classMap({ description: true, ...changed('description') })}>
									${type.description}
								</p>`
							: nothing
					}
					${
						compositions.length
							? html`<dl class="meta">
									<dt>
										${this.localize.termOrDefault('uSync_previewCompositions', 'Compositions')}
									</dt>
									<dd class=${classMap(changed('compositions'))}>
										${compositions.map(
											(c) =>
												html`<uui-tag
													look="outline"
													color=${c.isNew ? 'positive' : 'default'}
													>${c.name}${
														c.isNew
															? ` (${this.localize.termOrDefault('uSync_previewNew', 'new')})`
															: ''
													}</uui-tag
												> `,
										)}
									</dd>
								</dl>`
							: nothing
					}
				</div>
			</uui-box>
		`;
	}

	#renderTabButton(tab: USyncLayoutTab, active: boolean) {
		const status = this.#tabStatus(tab);
		const label =
			tab.name || this.localize.termOrDefault('uSync_previewGenericTab', 'Generic');
		return html`<uui-tab
			label=${label}
			?active=${active}
			@click=${() => (this._activeTab = tab.name)}>
			<span class="tab-label">
				${label}
				${status ? html`<span class="dot ${status}" title=${status}></span>` : nothing}
			</span>
		</uui-tab>`;
	}

	#tabStatus(tab: USyncLayoutTab) {
		const own = tab.ids.map((id) => this.#diff?.containers.get(id)).find(Boolean);
		if (own) return own.status;

		const children = [
			...tab.groups.flatMap((g) => g.ids.map((id) => this.#diff?.containers.get(id))),
			...tab.properties.map((p) => this.#diff?.properties.get(p.property.id)),
			...tab.groups.flatMap((g) =>
				g.properties.map((p) => this.#diff?.properties.get(p.property.id)),
			),
		];
		return children.some(Boolean) ? 'changed' : undefined;
	}

	#renderTab(tab: USyncLayoutTab) {
		// inert rather than readonly: most editors render only their value when
		// readonly, which is empty here, so you wouldn't see the editor at all.
		return html`
			<umb-property-dataset .value=${[]} inert>
				${
					tab.properties.length
						? html`<uui-box
								>${this.#renderProperties(tab.properties, this.#ownStatus(tab.ids))}</uui-box
							>`
						: nothing
				}
				${repeat(
					tab.groups,
					(group) => group.name,
					(group) => {
						const change = group.ids
							.map((id) => this.#diff?.containers.get(id))
							.find(Boolean);
						return html`<uui-box class=${change?.status ?? ''}>
							<div slot="headline" class="headline">
								${group.name}
								${group.inherited ? html`<small class="inherited">inherited</small>` : nothing}
								${this.#renderBadge(change)}
							</div>
							${
								group.properties.length
									? this.#renderProperties(
											group.properties,
											change?.status ?? this.#ownStatus(tab.ids),
										)
									: html`<p class="empty">
											${this.localize.termOrDefault('uSync_previewNoProperties', 'No properties')}
										</p>`
							}
						</uui-box>`;
					},
				)}
			</umb-property-dataset>
		`;
	}

	/** the status of a container itself (not its contents) */
	#ownStatus(ids: string[]) {
		return ids.map((id) => this.#diff?.containers.get(id)).find(Boolean)?.status;
	}

	/**
	 * @param parentStatus when the whole group (or tab) is added or removed, its
	 * properties are too, so they aren't marked one by one.
	 */
	#renderProperties(properties: USyncLayoutProperty[], parentStatus?: string) {
		const markItems = parentStatus !== 'added' && parentStatus !== 'removed';
		return repeat(
			properties,
			(entry) => entry.property.id,
			({ property: p, inheritedFrom }) => {
				const change = markItems ? this.#diff?.properties.get(p.id) : undefined;
				const dataType = this.#dataTypes.get(p.dataType.id);

				return html`<div class="property ${change?.status ?? ''}">
					<div class="property-meta">
						<code>${p.alias}</code>
						<span>${dataType?.name ?? p.dataType.id}</span>
						${
							this.#newDataTypes.has(p.dataType.id)
								? html`<uui-tag size="s" look="outline" color="positive"
										>${this.localize.termOrDefault('uSync_previewNewDataType', 'new data type')}</uui-tag
									>`
								: nothing
						}
						${inheritedFrom ? html`<small class="inherited">from ${inheritedFrom}</small>` : nothing}
						${p.variesByCulture ? html`<small>varies by culture</small>` : nothing}
						${p.variesBySegment ? html`<small>varies by segment</small>` : nothing}
						${this.#renderBadge(change)}
					</div>
					${
						dataType
							? html`<umb-property
									.alias=${p.alias}
									.label=${p.name}
									.description=${p.description ?? ''}
									.propertyEditorUiAlias=${dataType.editorUiAlias}
									.config=${dataType.values}
									.appearance=${p.appearance}
									.validation=${p.validation}></umb-property>`
							: html`<p class="empty">
									${p.name} -
									${this.localize.termOrDefault(
										'uSync_previewMissingDataType',
										'the data type for this property is not in the site yet',
									)}
								</p>`
					}
				</div>`;
			},
		);
	}

	#renderBadge(change: USyncPreviewItemChange | undefined) {
		if (!change) return nothing;
		const color =
			change.status === 'added'
				? 'positive'
				: change.status === 'removed'
					? 'danger'
					: 'warning';
		return html`<uui-tag size="s" color=${color} title=${change.fields.join(', ')}
			>${change.status}</uui-tag
		>`;
	}

	static styles = css`
		:host {
			display: block;
			container-type: inline-size;
		}

		/* the two sides share rows (headline, info, tabs, content), so the
		   tab bars and groups line up even when the info boxes differ. */
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

		/* info boxes match heights across the sides (but a lone "not here" box doesn't stretch). */
		.side > .info:not(:last-child) {
			align-self: stretch;
		}

		@container (max-width: 760px) {
			.sides {
				grid-template-columns: 1fr;
				grid-template-rows: none;
				row-gap: var(--uui-size-layout-1);
			}

			.side {
				display: flex;
				flex-direction: column;
				align-items: stretch;
				gap: var(--uui-size-space-4);
				grid-row: auto;
			}
		}

		umb-property-dataset {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
			min-width: 0;
		}

		.side-headline {
			margin: 0;
			font-size: var(--uui-type-small-size);
			text-transform: uppercase;
			letter-spacing: 0.04em;
			color: var(--uui-color-text-alt);
		}

		.headline {
			display: flex;
			align-items: center;
			flex-wrap: wrap;
			gap: var(--uui-size-space-2) var(--uui-size-space-3);
		}

		.headline code {
			font-size: var(--uui-type-small-size);
			color: var(--uui-color-text-alt);
		}

		.info-body {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
		}

		.tags {
			display: flex;
			flex-wrap: wrap;
			gap: var(--uui-size-space-2);
		}

		.description {
			margin: 0;
		}

		.meta {
			display: grid;
			grid-template-columns: max-content 1fr;
			align-items: center;
			gap: var(--uui-size-space-2) var(--uui-size-space-5);
			margin: 0;
		}

		.meta dt {
			font-weight: 700;
		}

		.meta dd {
			display: flex;
			flex-wrap: wrap;
			gap: var(--uui-size-space-2);
			margin: 0;
		}

		/* a changed field gets a soft highlight, spread with a shadow so the text doesn't move. */
		.changed:not(.property):not(uui-box) {
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

		.dot.added {
			background: var(--uui-color-positive);
		}

		.dot.removed {
			background: var(--uui-color-danger);
		}

		.dot.changed {
			background: var(--uui-color-warning);
		}

		uui-box.added {
			border-left: 3px solid var(--uui-color-positive);
		}

		uui-box.removed {
			border-left: 3px solid var(--uui-color-danger);
		}

		uui-box.changed {
			border-left: 3px solid var(--uui-color-warning);
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
			padding: var(--uui-size-space-4) var(--uui-size-space-5) 0;
			margin: 0 calc(-1 * var(--uui-size-space-5));
		}

		.property:first-child,
		.headline + .property {
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

		.property-meta {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-2) var(--uui-size-space-3);
			font-size: var(--uui-type-small-size);
			color: var(--uui-color-text-alt);
		}

		.property-meta code {
			color: var(--uui-color-text);
		}

		.inherited {
			font-style: italic;
			color: var(--uui-color-text-alt);
		}

		.empty {
			margin: 0;
			color: var(--uui-color-text-alt);
		}
	`;
}

function getSides(preview: SyncItemPreviewResponse): PreviewSides {
	switch (preview.entityType) {
		case 'document-type':
			return {
				current: preview.currentDocumentType ?? undefined,
				incoming: preview.incomingDocumentType ?? undefined,
				compositions: preview.compositionDocumentTypes,
			};
		case 'media-type':
			return {
				current: preview.currentMediaType ?? undefined,
				incoming: preview.incomingMediaType ?? undefined,
				compositions: preview.compositionMediaTypes,
			};
		case 'member-type':
			return {
				current: preview.currentMemberType ?? undefined,
				incoming: preview.incomingMemberType ?? undefined,
				compositions: preview.compositionMemberTypes,
			};
		default:
			return { compositions: [] };
	}
}

export default uSyncContentTypePreviewElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-content-type-preview': uSyncContentTypePreviewElement;
	}
}
