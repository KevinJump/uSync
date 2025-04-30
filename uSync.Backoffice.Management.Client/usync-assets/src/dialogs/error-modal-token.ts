import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { USyncActionView } from '../api';

export interface uSyncErrorModalData {
	action: USyncActionView;
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
