import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { USyncActionView } from '@jumoo/uSync';

export interface uSyncDetailsModalData {
	item: USyncActionView;
}

export interface uSyncDetailsModalValue {
	result: boolean;
}

export const USYNC_DETAILS_MODAL = new UmbModalToken<
	uSyncDetailsModalData,
	uSyncDetailsModalValue
>('usync.details.modal', {
	modal: {
		type: 'sidebar',
		size: 'large',
	},
});
