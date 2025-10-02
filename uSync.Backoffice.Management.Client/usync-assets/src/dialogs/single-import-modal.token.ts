import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { USyncActionView } from '../api';

export interface SyncImportSingleModalData {
	action: USyncActionView;
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
