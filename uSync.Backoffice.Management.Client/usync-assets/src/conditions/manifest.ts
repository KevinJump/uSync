import { ManifestCondition } from '@umbraco-cms/backoffice/extension-api';
import { SyncLegacyFilesCondition } from './legacy-files.condition';
import { uSyncConstants } from '../constants';

export const manifests: Array<ManifestCondition> = [
	{
		type: 'condition',
		alias: uSyncConstants.conditions.legacy,
		name: 'uSync Legacy Files Condition',
		api: SyncLegacyFilesCondition,
	},
];
