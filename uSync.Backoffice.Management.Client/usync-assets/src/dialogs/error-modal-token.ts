import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { uSyncActionView } from '../api';

export interface uSyncErrorModalData {
	action: uSyncActionView;
}

export interface uSyncErrorModalValue {
	result: boolean;
}

export const USYNC_ERROR_MODAL = new UmbModalToken<
	uSyncErrorModalData,
	uSyncErrorModalValue
>('usync.error.modal', {
	modal: {
		type: 'dialog',
	},
});
