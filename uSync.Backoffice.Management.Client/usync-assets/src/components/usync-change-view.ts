import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	classMap,
	css,
	customElement,
	html,
	property,
	repeat,
} from '@umbraco-cms/backoffice/external/lit';
import {
	ChangeDetailType,
	ChangeType,
	type USyncActionView,
	type USyncChange,
} from '@jumoo/uSync';
import './usync-change-detail.js';

/**
 * shows the change details for an item.
 */
@customElement('usync-change-view')
export class uSyncChangeView extends UmbLitElement {
	@property({ type: Object })
	item?: USyncActionView;

	#renderable() {
		return (this.item?.details ?? []).filter(
			(detail) => detail.change !== ChangeDetailType.NO_CHANGE,
		);
	}

	render() {
		if (this.item?.change === ChangeType.CREATE) {
			return this.render_create();
		}

		// note: this has to test the *renderable* details, not the raw count -
		// an unchanged item still arrives with a single `NoChange` detail, and
		// branching on the raw length would render an empty list instead of
		// the "no changes" message.
		const details = this.#renderable();
		if (details.length > 0) {
			return this.renderDetails(details);
		} else {
			return this.renderNoChanges();
		}
	}

	renderDetails(details: Array<USyncChange>) {
		// with a single change there's nothing to scan past, so open it
		// straight away; with more than one, start collapsed and let the user
		// open the ones they care about.
		const startOpen = details.length <= 1;

		return html`
			<div class="detail-list">
				${repeat(
					details,
					(detail, index) => `${index}:${detail.path}:${detail.name}`,
					(detail) =>
						html`<usync-change-detail
							.detail=${detail}
							?open=${startOpen}></usync-change-detail>`,
				)}
			</div>
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
				${this.renderMessage()}
			</div>
		`;
	}

	render_create() {
		return html`
			<div class="change-box">
				<h3>
					<umb-localize key="uSync_changeCreate">This item is being created</umb-localize>
				</h3>
			</div>
		`;
	}

	renderMessage() {
		const message =
			(this.item?.message?.length ?? 0) > 0
				? this.item?.message
				: this.item?.change == ChangeType.IMPORT
					? 'No changes were made but the item was imported'
					: 'There are pending changes for this item';

		const classes = { error: this.item?.success == false };
		return html`<div class="${classMap(classes)}">${message}</div> `;
	}

	static styles = css`
		:host {
			display: block;
			margin: 0;
		}

		.change-box {
			display: block;
			padding: var(
				--uui-box-header-padding,
				var(--uui-size-space-4, 12px) var(--uui-size-space-5, 18px)
			);
		}

		.change-box h3 {
			margin: 0;
		}

		.error {
			color: var(--uui-color-danger);
			margin-top: var(--uui-size-space-2);
		}

		.detail-list {
			display: flex;
			flex-direction: column;
			border-top: 1px solid var(--uui-color-border);
		}
	`;
}

export default uSyncChangeView;

declare global {
	interface HTMLElementTagNameMap {
		'usync-change-view': uSyncChangeView;
	}
}
