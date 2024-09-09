import { ManifestCondition } from '@umbraco-cms/backoffice/extension-api';
import { SyncLegacyFilesCondition } from '@jumoo/uSync';
import { uSyncConstants } from '@jumoo/uSync';

export const manifests: Array<ManifestCondition> = [
	{
		type: 'condition',
		alias: uSyncConstants.conditions.legacy,
		name: 'uSync Legacy Files Condition',
		api: SyncLegacyFilesCondition,
	},
];
