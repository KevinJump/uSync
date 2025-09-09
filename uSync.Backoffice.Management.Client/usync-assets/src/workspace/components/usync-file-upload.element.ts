import { uSyncActionRepository } from '@jumoo/uSync';
import {
	css,
	customElement,
	html,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbId } from '@umbraco-cms/backoffice/id';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	TemporaryFileStatus,
	UmbTemporaryFileManager,
	UmbTemporaryFileModel,
} from '@umbraco-cms/backoffice/temporary-file';
import { uSyncFilePickerChangeEvent, uSyncFilePickerUploadedEvent } from './events';
import { UUIButtonState } from '@umbraco-cms/backoffice/external/uui';

@customElement('usync-file-upload')
export class uSyncFileUploadElement extends UmbLitElement {
	#fileManager: UmbTemporaryFileManager;
	#repository: uSyncActionRepository;

	@state()
	buttonState?: UUIButtonState;

	@state()
	selected: File | null | undefined;

	@state()
	result: string | undefined;

	constructor() {
		super();

		this.#fileManager = new UmbTemporaryFileManager(this);
		this.#repository = new uSyncActionRepository(this);

		this.observe(this.#fileManager.queue, (value) => {
			value.forEach((file) => {
				if (file.status === TemporaryFileStatus.SUCCESS) {
					this.#uploadComplete(file.temporaryUnique);
					this.buttonState = 'success';
				}
			});
		});
	}

	#onUpload() {
		if (!this.selected) return;

		this.buttonState = 'waiting';

		const upload: UmbTemporaryFileModel = {
			temporaryUnique: UmbId.new(),
			file: this.selected,
			status: TemporaryFileStatus.WAITING,
		};

		this.#fileManager.upload([upload]);
	}

	async #uploadComplete(temporaryUnique: string) {
		const result = await this.#repository.processUpload(temporaryUnique);

		if (!result?.success) {
			console.log('upload error', result);
			return;
		}

		this.dispatchEvent(new uSyncFilePickerUploadedEvent(result));
	}

	#onFileChange(e: uSyncFilePickerChangeEvent) {
		this.selected = e.file;
	}

	render() {
		return html`${this.renderUploadForm()}`;
	}

	renderUploadForm() {
		return html` ${this.renderFile()} ${this.renderUploadButton()} `;
	}

	renderFile() {
		return html`
			<div class="upload-box">
				<usync-upload-file-picker
					label="Select uSync Zip file"
					@change=${this.#onFileChange}></usync-upload-file-picker>
			</div>
		`;
	}

	renderUploadButton() {
		if (!this.selected) return nothing;

		return html`<uui-button
			type="button"
			look="primary"
			@click="${this.#onUpload}"
			label="Upload"
			.state=${this.buttonState}></uui-button>`;
	}

	static styles = css`
		:host {
			display: flex;
			justify-content: space-between;
		}

		.upload-box {
			flex-grow: 2;
		}

		usync-upload-file-picker {
			width: 100%;
			flex-grow: 2;
		}
	`;
}

export default uSyncFileUploadElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-file-upload': uSyncFileUploadElement;
	}
}
