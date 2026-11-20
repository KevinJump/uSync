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
import { USYNC_DETAILS_MODAL, uSyncPreviewLabels, uSyncPreviewSource } from '@jumoo/uSync';
import { uSyncShowDetailEvent } from './events';
import { debounce } from '@umbraco-cms/backoffice/utils';
import type { UUIInputElement } from '@umbraco-cms/backoffice/external/uui';

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

	/**
	 * where the details dialog gets previews from, leave it unset to preview
	 * items from this site's uSync folders (see uSyncDetailsModalData).
	 */
	@property({ attribute: false })
	previewSource?: uSyncPreviewSource;

	/**
	 * the headlines over the two sides of the preview (see uSyncDetailsModalData).
	 */
	@property({ attribute: false })
	previewLabels?: uSyncPreviewLabels;

	@state()
	showAll: boolean = false;

	@state()
	changeCount = 0;

	@state()
	private _errorsOnly: boolean = false;

	@state()
	private _search: string = '';

	#toggleShowAll() {
		this.showAll = !this.showAll;
	}

	#toggleErrorsOnly() {
		this._errorsOnly = !this._errorsOnly;
	}

	#setSearch = debounce((value: string) => {
		this._search = value;
	}, 300);

	#onSearchInput(e: InputEvent) {
		this.#setSearch(((e.target as UUIInputElement).value as string) ?? '');
	}

	#hasFilters() {
		return this._errorsOnly || this._search.trim() !== '';
	}

	/**
	 * the results that pass the errors-only and search filters. the
	 * no-change / 'show all' rule isn't applied here, it stays where
	 * the rows are shown.
	 */
	#filteredResults() {
		const results = this.results || [];
		if (!this.#hasFilters()) return results;

		const search = this._search.trim().toLowerCase();
		return results.filter(
			(r) =>
				(!this._errorsOnly || r.success === false) &&
				(search === '' || (r.name ?? '').toLowerCase().includes(search)),
		);
	}

	async #showDetail(e: uSyncShowDetailEvent) {
		console.debug('Showing detail for action:', e.action);
		if (e.action.change == ChangeType.EXPORT) return;

		const detailsModal = this.#modalContext?.open(this, USYNC_DETAILS_MODAL, {
			data: {
				item: e.action,
				items: this.#visibleItems(),
				previewSource: this.previewSource,
				previewLabels: this.previewLabels,
				showActions: (item) =>
					!this.hideActions &&
					(item.change == ChangeType.CREATE ||
						item.change == ChangeType.UPDATE ||
						item.change == ChangeType.IMPORT),
			},
		});

		const data = await detailsModal?.onSubmit().catch(() => {
			return;
		});
		if (!data) return;
	}

	/**
	 * the items in the order they are shown in the list (grouped by type,
	 * no-change items only when 'show all' is on), so the details dialog
	 * can step through them. exports have no details, so are left out.
	 */
	#visibleItems() {
		const groups = this.groupBy(this.#filteredResults(), (result) => result.itemType);
		return Object.values(groups)
			.flat()
			.filter(
				(r) =>
					r.change !== ChangeType.EXPORT &&
					(this.showAll || r.change !== ChangeType.NO_CHANGE),
			);
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

		const groups = this.groupBy(this.#filteredResults(), (result) => result.itemType);
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

		const noMatch = this.#hasFilters() && groupsHtml.length === 0;

		return this.changeCount == 0 && !this.showAll
			? html`
					${this.renderResultBar(this.results?.length || 0, this.changeCount)}
					<div class="empty">
						<umb-localize key="uSync_noChange"></umb-localize>
					</div>
				`
			: html`<div id="result-box">
					${this.renderResultBar(this.results?.length || 0, this.changeCount)}
					${noMatch ? this.#renderNoMatch() : groupsHtml}
				</div>`;
	}

	renderResultBar(count: number, changes: number) {
		if (this.hideResultBar) return nothing;
		const localKey = changes === 0 ? 'uSync_noChangeCount' : 'uSync_changeCount';

		const searchLabel = this.localize.termOrDefault(
			'uSync_searchResults',
			'Search by name',
		);

		return html`<div class="result-header">
			<uui-toggle
				.label=${this.localize.termOrDefault('uSync_showAll', 'Show all items')}
				?checked=${this.showAll}
				@change=${this.#toggleShowAll}></uui-toggle>
			<div class="result-filters">
				${this.#renderChangeTypeCounts()} ${this.#renderFailedToggle()}
				<uui-input
					label=${searchLabel}
					placeholder=${searchLabel}
					.value=${this._search}
					@input=${this.#onSearchInput}>
					<uui-icon name="icon-search" slot="prepend"></uui-icon>
				</uui-input>
			</div>
			<umb-localize .key=${localKey} .args=${[count, changes]}
				>${changes}/${count} items</umb-localize
			>
		</div>`;
	}

	#renderNoMatch() {
		return html`<div class="empty">
			${this.localize.termOrDefault(
				'uSync_noMatchingItems',
				'No items match the current filters',
			)}
		</div>`;
	}

	#renderChangeTypeCounts() {
		const counts = new Map<string, number>();
		for (const result of this.results || []) {
			if (result.change === ChangeType.NO_CHANGE && !this.showAll) continue;
			counts.set(result.change, (counts.get(result.change) ?? 0) + 1);
		}

		return [...counts].map(([change, count]) => {
			const failure = this.#failureTypes.includes(change);
			return html`<uui-tag look="secondary" color=${failure ? 'danger' : 'default'}>
				${failure ? html`<uui-icon name="icon-alert"></uui-icon>` : nothing}
				${this.localize.termOrDefault('uSync_changeType' + change, change)}: ${count}
			</uui-tag>`;
		});
	}

	#failureTypes: Array<string> = [
		ChangeType.FAIL,
		ChangeType.IMPORT_FAIL,
		ChangeType.MISMATCH,
		ChangeType.PARENT_MISSING,
	];

	#renderFailedToggle() {
		const failed = this.results?.filter((r) => r.success === false).length ?? 0;
		if (failed === 0 && !this._errorsOnly) return nothing;

		const label = this.localize.termOrDefault(
			'uSync_failedCount',
			`${failed} failed`,
			failed,
		);

		return html`<uui-button
			compact
			color="danger"
			look=${this._errorsOnly ? 'primary' : 'outline'}
			label=${label}
			@click=${this.#toggleErrorsOnly}>
			<uui-icon name="icon-alert"></uui-icon> ${label}
		</uui-button>`;
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
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-4);
			justify-content: space-between;
			padding: var(--uui-size-space-4);
			border: 1px solid var(--uui-color-border);
			padding: var(--uui-size-space-4);
		}

		.result-filters {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-3);
		}

		.result-filters uui-input {
			width: 15rem;
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
