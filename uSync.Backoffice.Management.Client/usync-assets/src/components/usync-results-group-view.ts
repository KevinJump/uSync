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

	getChangeCount() {
		return this.results?.filter((r) => r.change !== ChangeType.NO_CHANGE).length;
	}

	render() {
		const changeCount = this.getChangeCount() ?? 0;

		if (changeCount === 0 && !this.showAll) return nothing;

		return html`
			<uui-box
				class=${classMap({
					has_changes: changeCount > 0,
				})}>
				<div
					class="summary ${when(this.expanded, () => 'expanded')}"
					@click=${() => (this.expanded = !this.expanded)}>
					<h4>${this.localize.term('uSync_' + this.groupName)}</h4>
					<div class="summary-right">
						<h4 class="count">${changeCount}/${this.results?.length}</h4>
						<uui-icon
							name="icon-play"
							class=${classMap({ expanded: this.expanded })}></uui-icon>
					</div>
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
			return html`<usync-result-row .result=${result}></usync-result-row>`;
		});

		return rowsHtml;
	}

	static styles = css`
		uui-box {
			cursor: pointer;
			--uui-box-default-padding: 0;
		}

		.expanded {
			border-bottom: 1px solid var(--uui-color-border);
		}

		.summary {
			display: flex;
			margin: 0;
			padding: 0 20px;
			justify-content: space-between;
		}

		.summary-right {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-2);
			color: var(--uui-color-border-emphasis);
		}

		.summary-right uui-icon {
			transform: rotate(90deg);
			transition: transform 0.5s cubic-bezier(0.42, 0, 0.37, 1.62);
		}

		.summary-right uui-icon.expanded {
			transform: rotate(-90deg);
			border-bottom: none;
		}

		.count {
			color: var(--uui-text);
		}
	`;
}

export default uSyncResultGroupView;

declare global {
	interface HTMLElementTagNameMap {
		'usync-result-group': uSyncResultGroupView;
	}
}
