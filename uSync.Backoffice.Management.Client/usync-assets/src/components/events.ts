import { USyncActionView } from '../api';

export const USYNC_SHOW_DETAIL_EVENT = 'show-detail';

export class uSyncShowDetailEvent extends Event {
	public constructor(action: USyncActionView) {
		super(USYNC_SHOW_DETAIL_EVENT, {
			bubbles: true,
			composed: true,
			cancelable: false,
		});

		this.action = action;
	}

	action: USyncActionView;
}


declare global {
	interface GlobalEventHandlersEventMap {
		[USYNC_SHOW_DETAIL_EVENT]: uSyncShowDetailEvent;
}