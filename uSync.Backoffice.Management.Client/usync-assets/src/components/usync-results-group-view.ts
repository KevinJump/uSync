import {
	classMap,
	css,
	customElement,
	html,
	nothing,
	property,
	state,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { ChangeType, USyncActionView } from '../api';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { USYNC_ERROR_MODAL } from '../dialogs';

@customElement('usync-result-group')
export class uSyncResultGroupView extends UmbLitElement {
	@state()
	expanded: boolean = false;

	@property({ type: Boolean })
	showAll: boolean = false;

	@property({ type: Array })
	results: Array<USyncActionView> = [];

	@property({ type: String })
	groupName: string = '';

	async #showDetail(action: USyncActionView) {
		if (action.change == ChangeType.NO_CHANGE) return;

		this.dispatchEvent(
			new CustomEvent<USyncActionView>('show-detail', { detail: action }),
		);
	}

	getChangeCount() {
		return this.results?.filter((r) => r.change !== ChangeType.NO_CHANGE).length;
	}

	render() {
		const changeCount = this.getChangeCount() ?? 0;

		if (changeCount === 0 && !this.showAll) return nothing;

		return html`
			<uui-box class=${classMap({ has_changes: changeCount > 0 })}>
				<div
					class="summary ${when(this.expanded, () => 'expanded')}"
					@click=${() => (this.expanded = !this.expanded)}>
					<h4>${this.localize.term('uSync_' + this.groupName)}</h4>
					<h4 class="count">${changeCount}/${this.results?.length}</h4>
				</div>
				<uui-table>
					${when(
						this.expanded == true,
						() => html`${this.renderGroupedRows(this.results)}`,
					)}
				</uui-table>
			</uui-box>
		`;
	}

	renderGroupedRows(results?: USyncActionView[]) {
		const rowsHtml = results?.map((result) => {
			if (!this.showAll && result.change == ChangeType.NO_CHANGE) return nothing;

			const icon =
				result.change == ChangeType.NO_CHANGE
					? 'icon-trafic'
					: result.success
						? 'icon-check color-green'
						: 'icon-wrong color-red';

			return html`
				<uui-table-row
					class=${classMap({ changerow: result.change != ChangeType.NO_CHANGE })}>
					<uui-table-cell class="icon-cell" .noPadding=${true}>
						<umb-icon .name=${icon}></umb-icon>
					</uui-table-cell>
					<uui-table-cell
						@click=${() => this.#showDetail(result)}
						.clipText=${true}
						style="--uui-table-cell-padding: var(--uui-size-space-2);">
						<div class="item-name">
							<div>${result.name}</div>
							<div>${this.renderMessage(result)}</div>
						</div>
						<div class="item-detail">
							<div>${result.itemType}</div>
							<div>${result.change}</div>
						</div>
					</uui-table-cell>
				</uui-table-row>
			`;
		});

		return html`${rowsHtml}`;
	}

	renderMessage(result: USyncActionView) {
		return (result.change != ChangeType.FAIL &&
			result.change != ChangeType.IMPORT_FAIL) ||
			!result.message
			? html`<em>${result.message}</em>`
			: html` <uui-button
					look="default"
					color="danger"
					label="View error"
					compact
					@click=${(e: Event) => this.#viewError(e, result)}></uui-button>`;
	}

	async #viewError(e: Event, result: USyncActionView) {
		e.stopPropagation();
		const modalContext = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		const modal = modalContext?.open(this, USYNC_ERROR_MODAL, {
			data: {
				action: result,
			},
		});

		const data = await modal?.onSubmit().catch(() => {
			return;
		});

		return data;
	}

	static styles = css`
		uui-box {
			cursor: pointer;
			--uui-box-default-padding: 1px 20px;
		}

		.summary {
			display: flex;
			margin: 0 -20px;
			padding: 0 20px;
			justify-content: space-between;
		}

		.expanded {
			border-bottom: 1px solid var(--uui-color-border);
		}

		.count {
			color: var(--uui-color-border);
		}

		.has_changes .count {
			color: var(--uui-text);
		}

		.changerow {
			cursor: pointer;
		}

		.icon-cell {
			width: var(--uui-size-8);
		}

		.item-name {
			display: flex;
			justify-content: space-between;
		}

		.item-detail {
			display: flex;
			justify-content: space-between;
			font-size: smaller;
			color: var(--uui-color-disabled-contrast);
		}

		uui-table-row:first-child uui-table-cell {
			border-top-color: transparent;
		}
	`;
}

export default uSyncResultGroupView;

declare global {
	interface HTMLElementTagNameMap {
		'usync-result-group': uSyncResultGroupView;
	}
}
