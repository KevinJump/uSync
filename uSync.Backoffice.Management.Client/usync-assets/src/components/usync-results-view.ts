import {
	LitElement,
	html,
	customElement,
	property,
	nothing,
	css,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { ChangeType, USYNC_ERROR_MODAL, uSyncActionView } from '@jumoo/uSync';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	UMB_MODAL_MANAGER_CONTEXT,
	UmbModalManagerContext,
} from '@umbraco-cms/backoffice/modal';
import { USYNC_DETAILS_MODAL } from '@jumoo/uSync';

@customElement('usync-results')
export class uSyncResultsView extends UmbElementMixin(LitElement) {
	#modalContext?: UmbModalManagerContext;

	constructor() {
		super();

		this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (_instance) => {
			this.#modalContext = _instance;
		});
	}

	@property({ type: Array })
	results: Array<uSyncActionView> | undefined = [];

	@state()
	showAll: boolean = false;

	@state()
	changeCount = 0;

	#toggleShowAll() {
		this.showAll = !this.showAll;
	}

	async #openDetailsView(result: uSyncActionView) {
		const detailsModal = this.#modalContext?.open(this, USYNC_DETAILS_MODAL, {
			data: {
				item: result,
			},
		});

		const data = await detailsModal?.onSubmit().catch(() => {
			return;
		});
		if (!data) return;
	}

	async #viewError(result: uSyncActionView) {
		const modal = this.#modalContext?.open(this, USYNC_ERROR_MODAL, {
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
		this.changeCount = 0;

		var rowsHtml = this.results?.map((result) => {
			if (this.showAll == false && result.change == 'NoChange') {
				return nothing;
			}

			this.changeCount++;

			const classes = result.success ? 'success' : 'error';

			return html`
				<uui-table-row class=${classes}>
					<uui-table-cell
						><uui-icon .name=${result.success ? 'icon-check' : 'icon-wrong'}></uui-icon
					></uui-table-cell>
					<uui-table-cell>${result.change}</uui-table-cell>
					<uui-table-cell>${result.itemType}</uui-table-cell>
					<uui-table-cell>${result.name}</uui-table-cell>
					<uui-table-cell
						>${result.details.length > 0
							? this.renderDetailsButton(result)
							: this.renderMessage(result)}</uui-table-cell
					>
				</uui-table-row>
			`;
		});

		return this.changeCount == 0
			? html`
					${this.renderResultBar(this.results?.length || 0)}
					<div class="empty">
						<umb-localize key="uSync_noChange"></umb-localize>
					</div>
				`
			: html`
					${this.renderResultBar(this.results?.length || 0)}
					<uui-table>
						<uui-table-head>
							<uui-table-head-cell>
								<umb-localize key="uSync_success">Success</umb-localize>
							</uui-table-head-cell>
							<uui-table-head-cell>
								<umb-localize key="uSync_change">Change</umb-localize>
							</uui-table-head-cell>
							<uui-table-head-cell>
								<umb-localzie key="uSync_changeType">Type</umb-localzie>
							</uui-table-head-cell>
							<uui-table-head-cell>
								<umb-localize key="uSync_changeName">Name</umb-localize>
							</uui-table-head-cell>
							<uui-table-head-cell>
								<umb-localize key="uSync_changeDetail">Detail</umb-localize>
							</uui-table-head-cell>
						</uui-table-head>

						${rowsHtml}
					</uui-table>
				`;
	}

	renderResultBar(count: number) {
		return html` <div class="result-header">
			<uui-toggle
				.label=${this.localize.term('uSync_showAll')}
				?checked=${this.showAll}
				@change=${this.#toggleShowAll}></uui-toggle>
			<umb-localize key="uSync_changeCount" .args=${[count]}>${count} items</umb-localize>
		</div>`;
	}

	renderDetailsButton(result: uSyncActionView) {
		return html`
			<uui-button
				look="default"
				color="positive"
				label="show details"
				compact
				@click=${() => this.#openDetailsView(result)}></uui-button>
		`;
	}

	renderMessage(result: uSyncActionView) {
		return (result.change != ChangeType.FAIL &&
			result.change != ChangeType.IMPORT_FAIL) ||
			!result.message
			? nothing
			: html` <uui-button
					look="default"
					color="warning"
					label="View error"
					compact
					@click=${() => this.#viewError(result)}></uui-button>`;
	}

	static styles = css`
		:host {
			display: block;
			margin: var(--uui-size-space-4) 0;
		}

		uui-table {
			position: relative;
			z-index: 100;
		}

		.result-header {
			display: flex;
			justify-content: space-between;
			margin-top: calc(var(--uui-size-space-4) * -1);
		}

		.empty {
			padding: var(--uui-size-20);
			font-size: var(--uui-type-h5-size);
			text-align: center;
			font-weight: 900;
		}

		.error {
			background-color: #fce4ec;
		}
	`;
}

export default uSyncResultsView;
