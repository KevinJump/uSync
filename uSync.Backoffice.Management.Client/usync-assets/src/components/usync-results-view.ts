import {
	LitElement,
	html,
	customElement,
	property,
	css,
	state,
	nothing,
} from '@umbraco-cms/backoffice/external/lit';
import { ChangeType, USyncActionView } from '@jumoo/uSync';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	UMB_MODAL_MANAGER_CONTEXT,
	UmbModalManagerContext,
} from '@umbraco-cms/backoffice/modal';
import { USYNC_DETAILS_MODAL } from '@jumoo/uSync';
import { uSyncShowDetailEvent } from './events';

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
	results: Array<USyncActionView> | undefined = [];

	@property({ type: Boolean })
	hideResultBar: boolean = false;

	@property({ type: Boolean })
	hideActions: boolean = false;

	@state()
	showAll: boolean = false;

	@state()
	changeCount = 0;

	#toggleShowAll() {
		this.showAll = !this.showAll;
	}

	async #showDetail(e: uSyncShowDetailEvent) {
		console.debug('Showing detail for action:', e.action);
		if (e.action.change == ChangeType.EXPORT) return;

		const detailsModal = this.#modalContext?.open(this, USYNC_DETAILS_MODAL, {
			data: {
				item: e.action,
				showActions:
					!this.hideActions &&
					(e.action.change == ChangeType.CREATE ||
						e.action.change == ChangeType.UPDATE ||
						e.action.change == ChangeType.IMPORT),
			},
		});

		const data = await detailsModal?.onSubmit().catch(() => {
			return;
		});
		if (!data) return;
	}

	groupBy<T>(arr: T[], fn: (item: T) => any) {
		return arr.reduce<Record<string, T[]>>((prev, curr) => {
			const groupKey = fn(curr);
			const group = prev[groupKey] || [];
			group.push(curr);
			return { ...prev, [groupKey]: group };
		}, {});
	}

	render() {
		this.changeCount =
			this.results?.filter((r) => r.change !== ChangeType.NO_CHANGE).length ?? 0;

		const groups = this.groupBy(this.results || [], (result) => result.itemType);
		const groupsHtml = [];

		for (const key in groups) {
			const groupChanges =
				groups[key].filter((r) => r.change !== ChangeType.NO_CHANGE).length ?? 0;
			if (groupChanges === 0 && !this.showAll) continue;

			const groupHtml = html`<usync-result-group
				.groupName=${key}
				.results=${groups[key]}
				.showAll=${this.showAll}
				@show-detail=${this.#showDetail}></usync-result-group> `;

			groupsHtml.push(groupHtml);
		}

		return this.changeCount == 0 && !this.showAll
			? html`
					${this.renderResultBar(this.results?.length || 0, this.changeCount)}
					<div class="empty">
						<umb-localize key="uSync_noChange"></umb-localize>
					</div>
				`
			: html`<div id="result-box">
					${this.renderResultBar(this.results?.length || 0, this.changeCount)}
					${groupsHtml}
				</div>`;
	}

	renderResultBar(count: number, changes: number) {
		if (this.hideResultBar) return nothing;
		const localKey = changes === 0 ? 'uSync_noChangeCount' : 'uSync_changeCount';

		return html`<div class="result-header">
			<uui-toggle
				.label=${this.localize.termOrDefault('uSync_showAll', 'Show all items')}
				?checked=${this.showAll}
				@change=${this.#toggleShowAll}></uui-toggle>
			<umb-localize .key=${localKey} .args=${[count, changes]}
				>${changes}/${count} items</umb-localize
			>
		</div>`;
	}

	static styles = css`
		:host {
			display: block;
			margin: var(--uui-size-space-4) 0;
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
		}

		#result-box {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
		}

		uui-table {
			position: relative;
			z-index: 100;
		}

		.result-header {
			display: flex;
			justify-content: space-between;
			padding: var(--uui-size-space-4);
			border: 1px solid var(--uui-color-border);
			padding: var(--uui-size-space-4);
		}

		.result-header h3 {
			margin: 0;
			padding: 0;
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
