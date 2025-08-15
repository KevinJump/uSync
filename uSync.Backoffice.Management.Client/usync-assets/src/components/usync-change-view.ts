import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	LitElement,
	css,
	customElement,
	html,
	property,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import { ChangeType, USyncActionView } from '@jumoo/uSync';
import { diffWords } from '@umbraco-cms/backoffice/utils';

/**
 * shows the change details for an item.
 */
@customElement('usync-change-view')
export class uSyncChangeView extends UmbElementMixin(LitElement) {
	@property({ type: Object })
	item?: USyncActionView;

	render() {
		if (this.item?.change == ChangeType.CREATE) {
			return this.render_create();
		}

		if (this.item?.details.length ?? 0 > 0) {
			return this.renderChangeTable();
		} else {
			return this.renderNoChanges();
		}
	}

	renderChangeTable() {
		return html`
			<uui-table>
				<uui-table-head>
					<uui-table-head-cell>
						<umb-localize key="uSync_changeAction">Action</umb-localize>
					</uui-table-head-cell>
					<uui-table-head-cell>
						<umb-localize key="uSync_changeItem">Item</umb-localize>
					</uui-table-head-cell>
					<uui-table-head-cell>
						<umb-localize key="uSync_changeDiffrence">Difference</umb-localize>
					</uui-table-head-cell>
				</uui-table-head>
				${this.render_details()}
			</uui-table>
		`;
	}

	renderNoChanges() {
		return html`
			<div class="change-box">
				<h3>
					<umb-localize key="uSync_noChanges${this.item?.change}"
						>No changes</umb-localize
					>
				</h3>
				${when(
					this.item?.change == ChangeType.IMPORT,
					() =>
						html`<div>
							${(this.item?.message ?? '').length > 0
								? this.item?.message
								: 'Item was imported but no properties where changed '}
						</div>`,
				)}
			</div>
		`;
	}

	render_create() {
		return html`
			<h1>
				<umb-localize key="uSync_changeCreate">This item is being created</umb-localize>
			</h1>
		`;
	}

	#getJsonOrString(value: string | null | undefined) {
		try {
			return JSON.stringify(JSON.parse(value ?? ''), null, 1);
		} catch {
			return value ?? '';
		}
	}

	render_details() {
		var changesHtml = this.item?.details.map((detail) => {
			const oldValue = this.#getJsonOrString(detail.oldValue);
			const newValue = this.#getJsonOrString(detail.newValue);
			const changes = diffWords(oldValue, newValue);

			const changeHtml = changes.map((change: any) => {
				if (change.added) {
					return html`<ins>${change.value}</ins>`;
				} else if (change.removed) {
					return html`<del>${change.value}</del>`;
				} else {
					return html`<span>${change.value}</span>`;
				}
			});

			return html`
				<uui-table-row>
					<uui-table-cell>${detail.name}</uui-table-cell>
					<uui-table-cell>${detail.change}</uui-table-cell>
					<uui-table-cell class="detail-data">
						<pre>${changeHtml}</pre>
					</uui-table-cell>
				</uui-table-row>
			`;
		});

		return changesHtml;
	}

	render_changes() {}

	static styles = css`
		:host {
			display: block;
			margin: var(--uui-size-space-4) 0;
		}

		.change-box {
			padding: var(
				--uui-box-header-padding,
				var(--uui-size-space-4, 12px) var(--uui-size-space-5, 18px)
			);
		}
		.change-box h3 {
			margin: 0;
		}

		uui-table-cell {
			vertical-align: top;
		}

		uui-table-cell pre {
			margin: 0;
			padding: 0;
		}

		pre ins {
			color: var(--uui-color-positive);
		}

		pre del {
			color: var(--uui-color-danger);
		}
	`;
}

export default uSyncChangeView;
