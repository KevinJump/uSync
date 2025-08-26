import { SyncLegacyFilesCondition } from '@jumoo/uSync';
import { uSyncConstants } from '@jumoo/uSync';
import { USYNC_CONDITION_NEW_SECTION } from './constants';
import { SyncNewSectionCondition } from './new-section.condition';

export const manifests: UmbExtensionManifest[] = [
	{
		type: 'condition',
		alias: uSyncConstants.conditions.legacy,
		name: 'uSync Legacy Files Condition',
		api: SyncLegacyFilesCondition,
	},
	{
		type: 'condition',
		alias: USYNC_CONDITION_NEW_SECTION,
		name: 'uSync New Section Condition',
		api: SyncNewSectionCondition,
	},
];
