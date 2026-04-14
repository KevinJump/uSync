import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { customElement, property } from 'lit/decorators.js';
import { SyncFileVersionCheckResult } from '../api';
import { css, CSSResultGroup, html, nothing } from '@umbraco-cms/backoffice/external/lit';

@customElement('usync-sync-file-info')
export class uSyncSyncFileInfoView extends UmbLitElement {
	@property({ type: Object })
	_syncFileInfo?: SyncFileVersionCheckResult;

	render() {
		if (!this._syncFileInfo) return nothing;
		return html`${this.#renderHmacMismatch()} ${this.#renderFormatMismatch()}`;
	}

	#renderHmacMismatch() {
		if (!this._syncFileInfo) return nothing;
		if (this._syncFileInfo.hmacMatch) return nothing;
		return html`<div class="banner">
			<umb-localize key="uSync_hmacMismatch"></umb-localize>
		</div>`;
	}

	#renderFormatMismatch() {
		if (!this._syncFileInfo) return nothing;
		if (this._syncFileInfo.isCurrent) return nothing;
		return html`<div class="banner">
			<umb-localize key="uSync_formatMismatch"></umb-localize>
		</div>`;
	}

	static override styles? = [
		css`
			.banner {
				padding: var(--uui-size-space-2) var(--uui-size-space-4);
				margin: 0 0 var(--uui-size-space-4);
				background-color: var(--uui-color-surface);
			}
		`,
	];
}

export default uSyncSyncFileInfoView;

declare global {
	interface HTMLElementTagNameMap {
		'usync-sync-file-info': uSyncSyncFileInfoView;
	}
}
