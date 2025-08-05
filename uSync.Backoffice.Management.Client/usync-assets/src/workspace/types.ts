import { SyncActionGroup } from '@jumoo/uSync';

/**
 * Options passed to the performAction method on the workspace context
 */
export type SyncPerformActionOptions = {
	setName: string;
	group: SyncActionGroup;
	action: string;
	force: boolean;
	clean: boolean;
	file: boolean;
};
