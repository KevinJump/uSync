import {
	css,
	customElement,
	html,
	state,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UploadImportResult } from '../../api';
import { uSyncFilePickerUploadedEvent } from '../components/events';

@customElement('usync-import-dialog')
export class uSyncImportModalDialog extends UmbModalBaseElement<any, any> {
	@state()
	result: UploadImportResult | undefined;

	#onClose() {
		this.modalContext?.reject();
	}

	#onImport() {
		this.value = this.result?.success;
		this.modalContext?.submit();
	}

	#onUploaded(e: uSyncFilePickerUploadedEvent) {
		this.result = e.result;
	}

	render() {
		return html`
			<umb-body-layout .headline=${this.localize.termOrDefault('uSync_importHeader', 'Import from file')}>
				${this.renderForm()} ${this.renderResult()}
			</umb-body-layout>
		`;
	}

	renderForm() {
		if (this.result !== undefined) return;

		return html` ${this.localize.termOrDefault('uSync_uploadIntro', 'Select a zip file containing uSync files that you want to upload')}
			<usync-file-upload @uploaded=${this.#onUploaded}></usync-file-upload>
			<div slot="actions">
				<uui-button
					id="cancel"
					.label=${this.localize.term('general_close')}
					@click="${this.#onClose}"></uui-button>
			</div>`;
	}

	renderResult() {
		if (this.result == undefined) return;

		return html`${when(
				this.result.success,
				() => html`${this.localize.termOrDefault('uSync_uploadSuccess', 'The files have been uploaded and extracted to the uSync folder')}`,
				() => html`${this.localize.termOrDefault('uSync_uploadError', 'There was an error uploading the files')} ${this.result?.errors}`,
			)}
			<div slot="actions">
				<uui-button id="continue" label="Import" @click="${this.#onImport}"></uui-button>
			</div>`;
	}

	static styles = css`
		umb-body-layout {
			max-width: 450px;
		}

		usync-file-upload {
			padding: 10px 0;
		}
	`;
}

export default uSyncImportModalDialog;
