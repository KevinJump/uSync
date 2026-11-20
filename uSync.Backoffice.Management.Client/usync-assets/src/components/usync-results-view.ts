import {
	LitElement,
	html,
	customElement,
	property,
	css,
	state,
	nothing,
	repeat,
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
	private _filtersOpen: boolean = false;

	/**
	 * the change types picked in the filter row, empty means show every type.
	 */
	@state()
	private _changeTypes: Set<string> = new Set();

	@state()
	private _search: string = '';

	#toggleShowAll() {
		this.showAll = !this.showAll;

		// no-change items are hidden without 'show all', so a no-change filter can't match anything.
		if (!this.showAll && this._changeTypes.has(ChangeType.NO_CHANGE)) {
			this.#toggleChangeType(ChangeType.NO_CHANGE);
		}
	}

	#toggleFilters() {
		this._filtersOpen = !this._filtersOpen;
	}

	#toggleChangeType(change: string) {
		const changeTypes = new Set(this._changeTypes);
		if (!changeTypes.delete(change)) changeTypes.add(change);
		this._changeTypes = changeTypes;
	}

	#setSearch = debounce((value: string) => {
		this._search = value;
	}, 300);

	#onSearchInput(e: InputEvent) {
		this.#setSearch(((e.target as UUIInputElement).value as string) ?? '');
	}

	#hasFilters() {
		if (this.hideResultBar) return false;
		return this._changeTypes.size > 0 || this._search.trim() !== '';
	}

	/**
	 * the results that pass the change type and search filters. the
	 * no-change / 'show all' rule isn't applied here, it stays where
	 * the rows are shown.
	 */
	#filteredResults() {
		const results = this.results || [];
		if (!this.#hasFilters()) return results;

		const search = this._search.trim().toLowerCase();
		return results.filter(
			(r) =>
				(this._changeTypes.size === 0 || this._changeTypes.has(r.change)) &&
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

		const groups = Object.entries(
			this.groupBy(this.#filteredResults(), (result) => result.itemType),
		).filter(
			([, results]) =>
				this.showAll || results.some((r) => r.change !== ChangeType.NO_CHANGE),
		);

		const noChange = this.changeCount == 0 && !this.showAll;
		const noMatch = this.#hasFilters() && groups.length === 0;

		// one template for both states, so the header (and the search box) isn't re-created.
		return html`<div id="result-box">
			${this.renderResultBar(this.results?.length || 0, this.changeCount)}
			${noChange
				? this.#renderNoChange()
				: noMatch
					? this.#renderNoMatch()
					: this.#renderGroups(groups)}
		</div>`;
	}

	/**
	 * keyed on the group name, so each group keeps its own element (and its
	 * expanded state) when the filters add or remove the groups before it.
	 */
	#renderGroups(groups: Array<[string, Array<USyncActionView>]>) {
		return repeat(
			groups,
			([key]) => key,
			([key, results]) =>
				html`<usync-result-group
					.groupName=${key}
					.results=${results}
					.showAll=${this.showAll}
					@show-detail=${this.#showDetail}></usync-result-group>`,
		);
	}

	#renderNoChange() {
		return html`<div class="empty">
			<umb-localize key="uSync_noChange"></umb-localize>
		</div>`;
	}

	renderResultBar(count: number, changes: number) {
		if (this.hideResultBar) return nothing;
		const localKey = changes === 0 ? 'uSync_noChangeCount' : 'uSync_changeCount';

		const searchLabel = this.localize.termOrDefault(
			'uSync_searchResults',
			'Search by name',
		);

		const filterLabel = this.localize.termOrDefault('uSync_filterResults', 'Filter results');

		return html`<div class="result-header">
			<div class="result-summary">
				<uui-toggle
					.label=${this.localize.termOrDefault('uSync_showAll', 'Show all items')}
					?checked=${this.showAll}
					@change=${this.#toggleShowAll}></uui-toggle>
				<div class="result-summary-right">
					<umb-localize .key=${localKey} .args=${[count, changes]}
						>${changes}/${count} items</umb-localize
					>
					<uui-button
						compact
						label=${filterLabel}
						title=${filterLabel}
						aria-expanded=${this._filtersOpen ? 'true' : 'false'}
						@click=${this.#toggleFilters}>
						<uui-icon
							name="icon-navigation-down"
							class="expand-arrow ${this._filtersOpen ? 'open' : ''}"></uui-icon>
					</uui-button>
				</div>
			</div>
			${this._filtersOpen
				? html`<div class="result-filters">
						<div class="change-types">${this.#renderChangeTypeCounts()}</div>
						<uui-input
							label=${searchLabel}
							placeholder=${searchLabel}
							.value=${this._search}
							@input=${this.#onSearchInput}>
							<uui-icon name="icon-search" slot="prepend"></uui-icon>
						</uui-input>
					</div>`
				: nothing}
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
			const selected = this._changeTypes.has(change);
			return html`<button
				type="button"
				class="change-type"
				aria-pressed=${selected ? 'true' : 'false'}
				@click=${() => this.#toggleChangeType(change)}>
				<uui-tag
					look=${selected ? 'primary' : 'secondary'}
					color=${failure ? 'danger' : 'default'}>
					${failure ? html`<uui-icon name="icon-alert"></uui-icon>` : nothing}
					${this.localize.termOrDefault('uSync_changeType' + change, change)}: ${count}
				</uui-tag>
			</button>`;
		});
	}

	#failureTypes: Array<string> = [
		ChangeType.FAIL,
		ChangeType.IMPORT_FAIL,
		ChangeType.MISMATCH,
		ChangeType.PARENT_MISSING,
	];

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
			border: 1px solid var(--uui-color-border);
		}

		.result-summary {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: var(--uui-size-space-4);
			padding: var(--uui-size-space-3) var(--uui-size-space-4);
		}

		.result-summary-right {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
		}

		.expand-arrow {
			transition: transform 200ms ease-in-out;
		}

		.expand-arrow.open {
			transform: rotate(180deg);
		}

		.result-filters {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			justify-content: space-between;
			gap: var(--uui-size-space-3);
			padding: var(--uui-size-space-3) var(--uui-size-space-4);
			border-top: 1px solid var(--uui-color-border);
			animation: filters-in 200ms ease-out;
		}

		@keyframes filters-in {
			from {
				opacity: 0;
				transform: translateY(-4px);
			}
			to {
				opacity: 1;
				transform: none;
			}
		}

		.change-types {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-2);
		}

		button.change-type {
			all: unset;
			cursor: pointer;
			border-radius: var(--uui-border-radius);
		}

		button.change-type:focus-visible {
			outline: 2px solid var(--uui-color-focus);
			outline-offset: 2px;
		}

		button.change-type:hover uui-tag {
			filter: brightness(0.95);
		}

		.result-filters uui-input {
			width: 15rem;
			--uui-input-height: var(--uui-size-10);
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
