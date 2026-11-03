import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { SyncItemPreviewResponse, USyncActionView, uSyncPreviewLabels } from '@jumoo/uSync';

/**
 * where the details dialog gets the preview of an item from.
 *
 * - `false`: no preview, the dialog shows just the diff.
 * - a function: the caller loads it, for items that aren't in this site's
 *   uSync folders (e.g. a sync pack). return undefined when there isn't one.
 */
export type uSyncPreviewSource =
	| false
	| ((item: USyncActionView) => Promise<SyncItemPreviewResponse | undefined>);

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

	/**
	 * where the preview comes from, when it's not set the dialog previews
	 * the item from this site's uSync folders.
	 */
	previewSource?: uSyncPreviewSource;

	/**
	 * the headlines over the two sides of the preview, for a previewSource whose
	 * items don't come from this site's uSync folders.
	 */
	previewLabels?: uSyncPreviewLabels;
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
