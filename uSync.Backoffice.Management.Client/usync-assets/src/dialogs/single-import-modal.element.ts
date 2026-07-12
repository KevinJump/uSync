import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import {
	SyncImportSingleModalData,
	SyncImportSingleModalResult,
} from './single-import-modal.token';
import { customElement, state } from 'lit/decorators.js';
import { css, html } from 'lit';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';
import { USYNC_CORE_CONTEXT_TOKEN, uSyncWorkspaceContext } from '../workspace';
import { USyncAction } from '../api';
import { when } from '@umbraco-cms/backoffice/external/lit';

@customElement('usync-import-single-modal')
export default class SyncImportSingleModalElement extends UmbModalBaseElement<
	SyncImportSingleModalData,
	SyncImportSingleModalResult
> {
	#actionContext?: uSyncWorkspaceContext;

	@state()
	importState: UUIButtonState;

	@state()
	result?: USyncAction;

	constructor() {
		super();

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			if (!_instance) return;
			this.#actionContext = _instance;
		});
	}

	#onClose() {
		this.modalContext?.reject();
	}

	async #doImport() {
		if (!this.data?.action) return;
		this.importState = 'waiting';

		const result = await this.#actionContext?.importSingle(this.data.action);
		this.result = result?.data;
		if (result?.data?.success) this.importState = 'success';
		else this.importState = 'failed';
	}

	render() {
		return html`<umb-body-layout
			.headline=${`Import : ${this.data?.action.name ?? ''} [${this.data?.action.itemType}]`}>
			${this.renderResult()}
			<div slot="actions">
				<uui-button
					id="cancel"
					look="outline"
					.label=${this.localize.term('general_close')}
					@click="${this.#onClose}"></uui-button>
				${when(
					!this.result,
					() =>
						html` <uui-button
							id="import"
							look="primary"
							color="positive"
							type="button"
							.state=${this.importState}
							.label=${this.localize.termOrDefault('uSync_importSingle', 'Import')}
							@click="${this.#doImport}"></uui-button>`,
				)}
			</div>
		</umb-body-layout>`;
	}

	renderResult() {
		if (!this.result)
			return html` <strong>
				<umb-localize key="uSync_importSingleWarning"></umb-localize>
			</strong>`;

		if (this.result.success) {
			return html`<div>
				<umb-localize key="uSync_importSingleSuccess"></umb-localize>
			</div>`;
		} else {
			return html`<div>
				<umb-localize
					key="uSync_importSingleFailed"
					.args=${[this.result.message]}></umb-localize>
			</div>`;
		}
	}

	static styles = css`
		umb-body-layout {
			min-width: 450px;
			max-width: 450px;
		}
	`;
}
