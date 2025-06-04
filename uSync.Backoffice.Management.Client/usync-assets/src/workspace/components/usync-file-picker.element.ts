import {
	css,
	customElement,
	html,
	nothing,
	property,
	query,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { uSyncFilePickerChangeEvent } from './events';

@customElement('usync-upload-file-picker')
export class uSyncFilePicker extends UmbLitElement {
	@property({ type: String })
	label: string = 'Upload';

	@property({ type: String })
	accept: string = '';

	@query('#file')
	_input!: HTMLInputElement;

	@state()
	private _file: File | undefined;

	private async _getFile(fileEntry: FileSystemFileEntry) {
		return await new Promise<File>((resolve, reject) => {
			fileEntry.file(resolve, reject);
		});
	}

	private async _onFilePickerChange() {
		const files = this._input.files ? Array.from(this._input.files) : [];

		const entry = files[0];
		const isFile = entry instanceof File;
		const file = isFile ? entry : await this._getFile(entry);

		this._file = file;
		this._dispachChangeEvent();
	}

	private async _removeFile() {
		this._file = undefined;
		this._input.value = '';
		this._dispachChangeEvent();
	}

	private _onUpload() {
		this._input.click();
	}

	private _dispachChangeEvent() {
		this.dispatchEvent(new uSyncFilePickerChangeEvent(this._file));
	}

	render() {
		return html`<input
				@click=${(e: Event) => e.stopPropagation()}
				type="file"
				id="file"
				this.accept=${this.accept}
				@change=${this._onFilePickerChange} />
			${this._renderFile()} ${this._renderButton()}`;
	}

	_renderFile() {
		if (!this._file) return nothing;

		return html` <div class="file">
			<div>${this._file.name}</div>
			<uui-button
				@click="${() => this._removeFile()}"
				compact
				color="danger"
				label="Remove">
				<umb-icon name="icon-trash"></umb-icon>
			</uui-button>
		</div>`;
	}

	_renderButton() {
		return this._file
			? nothing
			: html` <uui-button
					id="add-button"
					look="placeholder"
					label=${this.label}
					@click="${this._onUpload}"></uui-button>`;
	}

	static styles = [
		css`
			.file {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-2);
			}

			#file {
				display: none;
			}

			#add-button {
				width: 100%;
			}
		`,
	];
}

export default uSyncFilePicker;

declare global {
	interface HTMLElementTagNameMap {
		'usync-upload-file-picker': uSyncFilePicker;
	}
}
