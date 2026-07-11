import { css, customElement, html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement, UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { SyncLegacyCheckResponse } from '@jumoo/uSync';

@customElement('usync-legacy-modal')
export class uSyncLegacyModalElement extends UmbModalBaseElement<
	SyncLegacyCheckResponse,
	string
> {
	#onClose() {
		this.modalContext?.reject();
	}

	render() {
		return html`
			<umb-body-layout headline="Legacy uSync folder detected">
				<div class="content">
					${this.renderLegacyFolder(this.data?.legacyFolder)}
					${this.renderLegacyTypes(this.data?.legacyTypes)} ${this.renderCopy()}
				</div>
				<div slot="actions">
					<uui-button id="cancel" label="close" @click=${this.#onClose}>
						${this.localize.term('general_close')}
					</uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	renderLegacyFolder(folder: string | null | undefined) {
		return folder === undefined || folder === null
			? nothing
			: html`${this.localize.termOrDefault('uSync.legacyInfo', 'uSync has found a legacy uSync folder', folder)}`;
	}

	renderLegacyTypes(legacyTypes: Array<string> | undefined) {
		if (legacyTypes == undefined || legacyTypes.length == 0) {
			return nothing;
		}

		const legacyTypeHtml = this.data?.legacyTypes.map((datatype) => {
			return html`<li>${datatype}</li>`;
		});

		return html`<div>
			${this.localize.termOrDefault('uSync.legacyObsolete', 'Obsolete DataTypes', legacyTypeHtml)}
		</div>`;
	}

	renderCopy() {
		return html`${this.localize.termOrDefault('uSync.legacyCopy', 'Copy to uSync folder', this.data?.legacyFolder ?? 'uSync/v15')} `;
	}

	static styles = css`
		.content {
			margin: -10px 0;
		}

		em {
			color: var(--uui-color-positive);
		}
	`;
}

export default uSyncLegacyModalElement;

export const USYNC_LEGACY_MODAL = new UmbModalToken<SyncLegacyCheckResponse, string>(
	'usync.legacy.modal',
	{
		modal: {
			type: 'dialog',
			size: 'small',
		},
	},
);
