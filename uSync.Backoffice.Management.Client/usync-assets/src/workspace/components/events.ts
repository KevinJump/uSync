import { SyncActionButton, SyncActionGroup, UploadImportResult } from '../../api';

export const USYNC_FILE_PICKER_CHANGE_EVENT = 'change';
export const USYNC_FILE_PICKER_UPLOADED_EVENT = 'uploaded';

/**
 * 'change' event fires when the file input changes, either by selecting a new file or removing the current one.
 */
export class uSyncFilePickerChangeEvent extends Event {
	public constructor(file: File | null | undefined) {
		super(USYNC_FILE_PICKER_CHANGE_EVENT, {
			bubbles: true,
			composed: true,
			cancelable: false,
		});

		this.file = file;
	}

	file: File | null | undefined;
}

/**
 * 'uploaded' event fires when a file has been successfully uploaded and extracted.
 */
export class uSyncFilePickerUploadedEvent extends Event {
	public constructor(result: UploadImportResult) {
		super(USYNC_FILE_PICKER_UPLOADED_EVENT, {
			bubbles: true,
			composed: true,
			cancelable: false,
		});

		this.result = result;
	}

	result: UploadImportResult;
}

export const USYNC_ACTION_BUTTON_CLICK_EVENT = 'usync-action-click';

export class uSyncActionButtonClickEvent extends Event {
	public constructor(button: SyncActionButton) {
		super(USYNC_ACTION_BUTTON_CLICK_EVENT, {
			bubbles: true,
			composed: true,
			cancelable: false,
		});

		this.button = button;
	}

	button: SyncActionButton;
}

export const USYNC_ACTION_PERFORM_EVENT = 'perform-action';

export class uSyncActionPerformEvent extends Event {
	public constructor(
		group: SyncActionGroup,
		key: string,
		force?: boolean,
		clean?: boolean,
		file?: boolean,
	) {
		super(USYNC_ACTION_PERFORM_EVENT, {
			bubbles: true,
			composed: true,
			cancelable: false,
		});

		this.group = group;
		this.key = key;
		this.force = force;
		this.clean = clean;
		this.file = file;
	}

	group: SyncActionGroup;
	key: string;
	force?: boolean;
	clean?: boolean;
	file?: boolean;
}
