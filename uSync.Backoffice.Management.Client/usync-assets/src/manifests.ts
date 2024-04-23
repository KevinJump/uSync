import { manifests as trees } from './tree/manifest.js';
import { manifests as workspace } from './workspace/manifest.js';
import { manifests as localization } from './lang/manifest.js';
import { manifests as dialogs } from './dialogs/manifest.js';

export const manifests = [...localization, ...trees, ...workspace, ...dialogs];
