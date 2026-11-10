import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { USyncActionView } from '../api';

export interface SyncImportSingleModalData {
	action: USyncActionView;

	/** the handler set the item came from, the import looks in its folders. */
	set?: string;
}

export interface SyncImportSingleModalResult {
	result: boolean;
}

export const USYNC_IMPORT_SINGLE_MODAL = new UmbModalToken<
	SyncImportSingleModalData,
	SyncImportSingleModalResult
>('usync.import.single.modal', {
	modal: {
		type: 'dialog',
	},
});
