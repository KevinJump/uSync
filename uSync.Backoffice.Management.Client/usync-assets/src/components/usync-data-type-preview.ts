import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	classMap,
	css,
	customElement,
	html,
	nothing,
	property,
} from '@umbraco-cms/backoffice/external/lit';
import '@umbraco-cms/backoffice/components';
import type { DataTypeResponseModel, SyncItemPreviewResponse } from '../api';
import { deepEqual, prettyJson } from '../utils/diff/index.js';

type PreviewSide = 'current' | 'incoming';

type ConfigStatus = 'added' | 'removed' | 'changed' | 'unchanged';

/**
 * Shows the current and incoming versions of a data type side by side,
 * rendered with the real property editor, so you can see what an import
 * will do before running it.
 */
@customElement('usync-data-type-preview')
export class uSyncDataTypePreviewElement extends UmbLitElement {
	@property({ type: Object })
	preview?: SyncItemPreviewResponse;

	render() {
		if (!this.preview) return nothing;

		const current = this.preview.currentDataType ?? undefined;
		const incoming = this.preview.incomingDataType ?? undefined;

		return html`
			<div class="sides">
				${this.#renderSide('current', current, incoming)}
				${this.#renderSide('incoming', incoming, current)}
			</div>
		`;
	}

	#renderSide(
		side: PreviewSide,
		dataType: DataTypeResponseModel | undefined,
		other: DataTypeResponseModel | undefined,
	) {
		const headline =
			side === 'current'
				? this.localize.termOrDefault('uSync_previewCurrent', 'Current (this site)')
				: this.localize.termOrDefault('uSync_previewIncoming', 'Incoming (uSync files)');

		return html`
			<div class="side">
				<h4 class="side-headline">${headline}</h4>
				${
					dataType
						? html`
								${this.#renderInfo(dataType, other)} ${this.#renderEditor(dataType)}
								${this.#renderConfig(dataType, other, side)}
							`
						: this.#renderMissing(side)
				}
			</div>
		`;
	}

	#renderMissing(side: PreviewSide) {
		const message =
			side === 'current'
				? this.localize.termOrDefault(
						'uSync_previewNotInSite',
						'This item does not exist in the site yet',
					)
				: this.preview?.isDelete
					? this.localize.termOrDefault(
							'uSync_previewDeleted',
							'This item will be deleted or renamed by the import',
						)
					: this.localize.termOrDefault(
							'uSync_previewFailed',
							'The incoming item could not be built for the preview',
						);

		return html`<uui-box><p class="empty">${message}</p></uui-box>`;
	}

	#renderInfo(dataType: DataTypeResponseModel, other?: DataTypeResponseModel) {
		const changed = (field: keyof DataTypeResponseModel) =>
			other !== undefined && other[field] !== dataType[field];

		return html`
			<uui-box>
				<div slot="headline" class="headline">
					<umb-icon name="icon-autofill"></umb-icon>
					<span class=${classMap({ changed: changed('name') })}>${dataType.name}</span>
				</div>
				<dl class="meta">
					<dt>
						${this.localize.termOrDefault('uSync_previewPropertyEditor', 'Property editor')}
					</dt>
					<dd class=${classMap({ changed: changed('editorAlias') })}>
						<code>${dataType.editorAlias}</code>
					</dd>
					<dt>
						${this.localize.termOrDefault(
							'uSync_previewPropertyEditorUi',
							'Property editor UI',
						)}
					</dt>
					<dd class=${classMap({ changed: changed('editorUiAlias') })}>
						<code>${dataType.editorUiAlias}</code>
					</dd>
				</dl>
			</uui-box>
		`;
	}

	#renderEditor(dataType: DataTypeResponseModel) {
		return html`
			<uui-box
				.headline=${this.localize.termOrDefault('uSync_previewEditor', 'Editor preview')}>
				<!-- inert rather than readonly: most editors render only their value when
				     readonly, which is empty here, so you wouldn't see the editor at all. -->
				<umb-property-dataset .value=${[]} inert>
					<umb-property
						alias="usyncPreview"
						.label=${dataType.name}
						.description=${this.localize.termOrDefault(
							'uSync_previewEditorDesc',
							'How this data type looks to editors',
						)}
						.propertyEditorUiAlias=${dataType.editorUiAlias}
						.config=${dataType.values}></umb-property>
				</umb-property-dataset>
			</uui-box>
		`;
	}

	#renderConfig(
		dataType: DataTypeResponseModel,
		other: DataTypeResponseModel | undefined,
		side: PreviewSide,
	) {
		const values = [...dataType.values].sort((a, b) => a.alias.localeCompare(b.alias));

		return html`
			<uui-box
				.headline=${this.localize.termOrDefault('uSync_previewConfiguration', 'Configuration')}>
				${
					values.length === 0
						? html`<p class="empty">
								${this.localize.termOrDefault(
									'uSync_previewNoConfiguration',
									'No configuration',
								)}
							</p>`
						: html`<table>
								${values.map((value) => {
									const status = configStatus(value.alias, value.value, other, side);
									return html`<tr class=${status}>
										<th><code>${value.alias}</code></th>
										<td><pre>${formatConfigValue(value.value)}</pre></td>
									</tr>`;
								})}
							</table>`
				}
			</uui-box>
		`;
	}

	static styles = css`
		:host {
			display: block;
		}

		.sides {
			display: grid;
			grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
			gap: var(--uui-size-space-5);
		}

		.side {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
			min-width: 0;
		}

		.side-headline {
			margin: 0;
		}

		.headline {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
		}

		.meta {
			display: grid;
			grid-template-columns: max-content 1fr;
			gap: var(--uui-size-space-2) var(--uui-size-space-5);
			margin: 0;
		}

		.meta dt {
			font-weight: 700;
		}

		.meta dd {
			margin: 0;
			overflow-wrap: anywhere;
		}

		.changed {
			background-color: var(--uui-color-warning);
			color: var(--uui-color-warning-contrast);
			padding: 0 var(--uui-size-space-1);
			border-radius: var(--uui-border-radius);
		}

		table {
			width: 100%;
			border-collapse: collapse;
		}

		tr + tr {
			border-top: 1px solid var(--uui-color-divider);
		}

		th,
		td {
			text-align: left;
			vertical-align: top;
			padding: var(--uui-size-space-2);
		}

		th {
			width: 30%;
			overflow-wrap: anywhere;
		}

		td pre {
			margin: 0;
			white-space: pre-wrap;
			word-break: break-word;
			font-size: 12px;
		}

		tr.changed {
			border-left: 3px solid var(--uui-color-warning);
			background: none;
			color: inherit;
		}

		tr.added {
			border-left: 3px solid var(--uui-color-positive);
		}

		tr.removed {
			border-left: 3px solid var(--uui-color-danger);
		}

		.empty {
			margin: 0;
			color: var(--uui-color-text-alt);
		}
	`;
}

/**
 * how a config value on this side compares to the other side. an alias
 * missing from the current side is being added, one missing from the
 * incoming side is being removed.
 */
function configStatus(
	alias: string,
	value: unknown,
	other: DataTypeResponseModel | undefined,
	side: PreviewSide,
): ConfigStatus {
	if (!other) return 'unchanged';
	const match = other.values.find((v) => v.alias === alias);
	if (!match) return side === 'incoming' ? 'added' : 'removed';
	return deepEqual(value, match.value) ? 'unchanged' : 'changed';
}

function formatConfigValue(value: unknown): string {
	return typeof value === 'string' ? value : prettyJson(value);
}

export default uSyncDataTypePreviewElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-data-type-preview': uSyncDataTypePreviewElement;
	}
}
