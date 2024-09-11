import { ManifestModal } from '@umbraco-cms/backoffice/extension-registry';

const modal: ManifestModal = {
	type: 'modal',
	alias: 'usync.details.modal',
	name: 'usync details modal',
	js: () => import('./details-modal-element.js'),
};

const legacyModal: ManifestModal = {
	type: 'modal',
	alias: 'usync.legacy.modal',
	name: 'uSync legacy modal',
	js: () => import('./legacy-modal-element.js'),
};

const errorModal: ManifestModal = {
	type: 'modal',
	alias: 'usync.error.modal',
	name: 'uSync error modal',
	js: () => import('./error-modal-element.js'),
};

export const manifests = [modal, legacyModal, errorModal];
