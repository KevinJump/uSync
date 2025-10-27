import { SyncHandlerSummary, USyncActionView } from '../api';

/**
 * @description format of the update message coming from uSync via SignalR.
 */
export type SyncUpdateMessage = {
	message: string;
	count: number;
	total: number;
};

export type SyncProgressSummary = {
	count: number;
	total: number;
	message: string;
	handlers: Array<SyncHandlerSummary>;
};

export type SyncCompleteMessage = {
	id: string;
	success: boolean;
	message: string;
	actions?: Array<USyncActionView>;
};
