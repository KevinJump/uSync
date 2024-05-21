import { manifests as trees } from './tree/manifest';
import { manifests as workspace } from './workspace/manifest';
import { manifests as localization } from './lang/manifest';
import { manifests as dialogs } from './dialogs/manifest';
import { manifests as conditions } from './conditions/manifest';

export const manifests = [
	...localization,
	...trees,
	...workspace,
	...dialogs,
	...conditions,
];
