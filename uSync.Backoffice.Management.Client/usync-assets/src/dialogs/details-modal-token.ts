import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { USyncActionView } from '@jumoo/uSync';

export interface uSyncDetailsModalData {
	item: USyncActionView;

	/**
	 * show the import button, either for every item or decided per item
	 * (when moving through `items` the answer can change from item to item).
	 */
	showActions?: boolean | ((item: USyncActionView) => boolean);

	/**
	 * the full list the item came from, when set the dialog can move
	 * to the previous / next item in the list.
	 */
	items?: Array<USyncActionView>;
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
