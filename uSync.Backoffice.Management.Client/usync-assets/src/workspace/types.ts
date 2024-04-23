import { SyncActionGroup } from '../api/index.js';

/**
 * Options passed to the performAction method on the workspace context
 */
export type SyncPerformActionOptions = {
	group: SyncActionGroup;
	action: string;
	force: boolean;
	clean: boolean;
};
