import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { ChangeType, USyncActionView } from '../api';
import { customElement, property } from 'lit/decorators.js';
import { classMap, css, html } from '@umbraco-cms/backoffice/external/lit';
import { uSyncShowDetailEvent } from './events';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { USYNC_ERROR_MODAL } from '../dialogs';

@customElement('usync-result-row')
export class uSyncResultRow extends UmbLitElement {
	@property({ type: Object })
	result?: USyncActionView;

	async #showDetail(action: USyncActionView) {
		if (action.change == ChangeType.NO_CHANGE || action.change == ChangeType.EXPORT)
			return;

		this.dispatchEvent(new uSyncShowDetailEvent(action));
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

	render() {
		if (!this.result) return null;
		const result = this.result;

		const icon =
			result.change == ChangeType.NO_CHANGE
				? 'icon-trafic'
				: result.success
					? 'icon-check color-green'
					: 'icon-wrong color-red';

		const noChange =
			result.change == ChangeType.NO_CHANGE || result.change == ChangeType.EXPORT;

		return html`<div
			class="${classMap({ no_change: noChange })} row"
			@click=${() => this.#showDetail(result)}>
			<div class="icon-cell" .noPadding=${true}>
				<umb-icon .name=${icon}></umb-icon>
			</div>
			<div class="row-content" .clipText=${true}>
				<div class="item-detail">
					<div class="item-change">${result.change}</div>
				</div>
				<div class="item-name">
					<div>${result.name}</div>
					<div>${this.renderMessage(result)}</div>
				</div>
			</div>
		</div>`;
	}

	renderMessage(result: USyncActionView) {
		return (result.change != ChangeType.FAIL &&
			result.change != ChangeType.IMPORT_FAIL) ||
			!result.message
			? html`<em>${result.message}</em>`
			: html` <uui-button
					look="outline"
					color="danger"
					label="View error"
					compact
					@click=${(e: Event) => this.#viewError(e, result)}></uui-button>`;
	}

	static styles = css`
		.row {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-5);
			padding: var(--uui-size-space-5);
			border-bottom: 1px solid var(--uui-color-border);
			cursor: pointer;
		}

		.row:hover {
			background-color: var(--uui-color-surface-alt);
		}

		.no_change {
			color: var(--uui-color-disabled-contrast);
			cursor: default;
		}

		.row-content {
			gap: 10px;
			flex-grow: 1;
			display: flex;
			align-items: center;
			justify-content: space-between;
		}

		.item-name {
			display: flex;
			font-weight: bold;
			align-items: center;
			flex-grow: 2;
			justify-content: space-between;
		}

		.item-detail {
			display: flex;
			flex-direction: column;
			font-size: smaller;
			min-width: 60px;
		}
	`;
}

export default uSyncResultRow;

declare global {
	interface HTMLElementTagNameMap {
		'usync-result-row': uSyncResultRow;
	}
}
