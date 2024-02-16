import { manifests as trees } from './tree/manifest';
import { manifests as workspace } from './workspace/manifest';
import { manifests as signalr } from './signalr/manifest';
import { manifests as localization } from './lang/manifest';

export const manifests = [
    ...localization,
    ...trees,
    ...workspace,
    ...signalr,
]